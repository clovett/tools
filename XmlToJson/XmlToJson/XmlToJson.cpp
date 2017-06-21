// XmlToJson.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "json.hpp"
#include <Windows.h>
#include <xmllite.h>
#include <Shlwapi.h>
#include <string>
#include <codecvt>
#include <vector>

const std::string childrenTag = "#children";
const std::string textTag = "#text";
const std::string commentTag = "#comment";

int convertAttributes(IXmlReader* reader, nlohmann::json& element)
{
    HRESULT hr;
    LPCWSTR pwszPrefix;
    LPCWSTR pwszLocalName;
    LPCWSTR pwszValue;
    UINT cwchPrefix;
    std::wstring_convert<std::codecvt_utf8<wchar_t>, wchar_t> converter;

    hr = reader->MoveToFirstAttribute();
    while (hr == 0) {
        if (FAILED(hr = reader->GetPrefix(&pwszPrefix, &cwchPrefix))) {
            wprintf(L"Error getting prefix, error is %08.8lx", hr);
            return -1;
        }

        if (FAILED(hr = reader->GetLocalName(&pwszLocalName, NULL))) {
            wprintf(L"Error getting local name, error is %08.8lx", hr);
            return -1;
        }
        std::string name;
        if (cwchPrefix > 0) {
            name = converter.to_bytes(pwszPrefix) + "_" + converter.to_bytes(pwszLocalName);
        }
        else {
            name = converter.to_bytes(pwszLocalName);
        }

        if (FAILED(reader->GetValue(&pwszValue, NULL))) {
            pwszValue = L"";
        }

        element["@" + name] = converter.to_bytes(pwszValue);

        hr = reader->MoveToNextAttribute();
    }
    return 0;
}

bool addChildNode(std::vector<nlohmann::json*>& parents, const std::string& name, nlohmann::json* parent, nlohmann::json& child)
{

    bool isArray = false;
    if (parent->type() == nlohmann::detail::value_t::array)
    {
        nlohmann::json object;
        object[name] = child;
        parent->push_back(object);
        isArray = true;
    }
    else if (parent->count(childrenTag) == 1) {
        nlohmann::json object;
        object[name] = child;
        nlohmann::json::array_t* a = (*parent)[childrenTag].get_ptr<nlohmann::json::array_t*>();
        a->push_back(object);
        isArray = true;
    }
    else if (parent->count(name) == 0) {
        (*parent)[name] = child;
    }
    else {
        // need to turn the parent into an array.
        auto children = nlohmann::json::array();
        std::vector<std::string> toErase;
        bool hasAttributes = false;
        for (auto ptr = parent->begin(), end = parent->end(); ptr != end; ptr++) {
            std::string key = ptr.key();
            if (key[0] == '@') {
                // skip attributes
                hasAttributes = true;
            }
            else {
                nlohmann::json object;
                object[key] = ptr.value();
                children.push_back(object);
                toErase.push_back(key);
            }
        }
        for (auto ptr = toErase.begin(), end = toErase.end(); ptr != end; ptr++) {
            parent->erase(*ptr);
        }

        // now add the new child 
        {
            nlohmann::json object;
            object[name] = child;
            children.push_back(object);
        }

        if (!hasAttributes && parents.size() > 1) {
            // then we can flatten it.
            auto grandparent = parents[parents.size() - 1];
            for (auto ptr = grandparent->begin(), end = grandparent->end(); ptr != end; ptr++) {
                std::string key = ptr.key();
                if (&ptr.value() == parent) {
                    (*grandparent)[key] = children;
                    parent = &(*grandparent)[key];
                    break;
                }
            }
        }
        else {
            (*parent)[childrenTag] = children;
        }
        isArray = true;
    }
    return isArray;
}

void flatten(nlohmann::json& parent, const std::string& name, nlohmann::json& e) {
    int textNodes = 0;
    int otherNodes = 0;
    if (e.type() == nlohmann::detail::value_t::array)
    {
        // then we have an array of objects, each object has a single name. 
        for (auto ptr = e.begin(), end = e.end(); ptr != end; ptr++) {
            nlohmann::json& obj = ptr.value();
            if (obj.type() == nlohmann::detail::value_t::object)
            {
                for (auto p2 = obj.begin(), end = obj.end(); p2 != end; p2++) {
                    std::string key = p2.key();
                    nlohmann::json& value = p2.value();
                    if (key == textTag) {
                        textNodes++;
                    }
                    else {
                        otherNodes++;
                        flatten(e, key, value);
                    }
                }
            }
        }
    }
    else if (e.type() == nlohmann::detail::value_t::object) {
        for (auto ptr = e.begin(), end = e.end(); ptr != end; ptr++) {
            std::string key = ptr.key();
            nlohmann::json& value = ptr.value();
            if (key == textTag) {
                textNodes++;
            }
            else {
                otherNodes++;
                flatten(e, key, value);
            }
        }
    }
    if (textNodes == 1 && otherNodes == 0)
    {
        // flatten it
        parent[name] = e[textTag];
    }

}

int convert(char* filename, nlohmann::json& doc) {
    HRESULT hr;

    std::wstring_convert<std::codecvt_utf8<wchar_t>, wchar_t> converter;
    std::wstring wide_path = converter.from_bytes(filename);

    //Open read-only input stream  
    IStream* stream;
    if (FAILED(hr = SHCreateStreamOnFile(wide_path.c_str(), STGM_READ, &stream)))
    {
        wprintf(L"Error creating file reader, error is %08.8lx", hr);
        return -1;
    }

    IXmlReader* reader;
    if (FAILED(hr = CreateXmlReader(__uuidof(IXmlReader), (void**)&reader, NULL)))
    {
        wprintf(L"Error creating xml reader, error is %08.8lx", hr);
        return -1;
    }

    reader->SetInput(stream);

    // xml data fields
    UINT cwchPrefix;
    LPCWSTR pwszPrefix;
    LPCWSTR pwszLocalName;
    LPCWSTR pwszValue;
    UINT nAttrCount;

    XmlNodeType nodeType;
    std::string name;
    std::string text;

    doc.clear();

    nlohmann::json* parent = &doc; // current element
    std::vector<nlohmann::json*> parents;
    nlohmann::json* root = nullptr;
    std::string rootName;

    // Read through each node until the end  
    while (!reader->IsEOF())
    {
        hr = reader->Read(&nodeType);

        // Check if E_PENDING is returned and perform custom action  
        // This is a sample of how one might handle E_PENDING  
        if (hr == E_PENDING)
        {
            // Alert user to the pending notification  
            wprintf(L"Error pending, error is %08.8lx", hr);

            // As long as E_PENDING is returned keep trying to read  
            while (hr == E_PENDING) {
                ::SleepEx(1000, TRUE);
                hr = reader->Read(&nodeType);
            }

            continue;
        }

        UINT line;
        reader->GetLineNumber(&line);

        if (hr != S_OK)
            break;

        switch (nodeType) {

        case XmlNodeType_XmlDeclaration:
            //if (FAILED(hr = convertAttributes(pReader, element))) {
            //    wprintf(L"Error writing attributes, error is %08.8lx", hr);
            //    return -1;
            //}
            break;

        case XmlNodeType_Element:
            if (FAILED(hr = reader->GetPrefix(&pwszPrefix, &cwchPrefix))) {
                wprintf(L"Error getting prefix, error is %08.8lx", hr);
                return -1;
            }

            if (FAILED(hr = reader->GetLocalName(&pwszLocalName, NULL))) {
                wprintf(L"Error getting local name, error is %08.8lx", hr);
                return -1;
            }

            if (cwchPrefix > 0) {
                name = converter.to_bytes(pwszPrefix) + "_" + converter.to_bytes(pwszLocalName);
            }
            else {
                name = converter.to_bytes(pwszLocalName);
            }
            {
                nlohmann::json element;
                reader->GetAttributeCount(&nAttrCount);
                if (nAttrCount > 0) {
                    convertAttributes(reader, element);
                    if (FAILED(hr = reader->MoveToElement())) {
                        wprintf(L"Error moving to the element that owns the current attribute node, error is %08.8lx", hr);
                        return -1;
                    }
                }

                bool isArray = addChildNode(parents, name, parent, element);

                if (!reader->IsEmptyElement()) {
                    parents.push_back(parent);
                    if (parent->type() == nlohmann::detail::value_t::array)
                    {
                        parent = &(parent->at(parent->size() - 1));
                    }
                    else if (parent->count(childrenTag) == 1) {
                        nlohmann::json::array_t* a = (*parent)[childrenTag].get_ptr<nlohmann::json::array_t*>();
                        parent = &(a->at(a->size() - 1));
                    }
                    else {
                        parent = &(*parent)[name];
                    }
                    if (isArray) {
                        // then parent is wrapped in dummy object, we need to unwrap it!
                        parent = &(*parent)[name];
                    }

                    if (root == nullptr) {
                        root = parent;
                        rootName = name;
                    }

                }
            }
            break;

        case XmlNodeType_EndElement:
            // let's assume xml is well formed... (could check element names match).
            if (parents.size() > 0) {
                parent = parents[parents.size() - 1];
                parents.pop_back();
            }
            else {
                wprintf(L"Unexpected end element tag");
                return -1;
            }
            break;

        case XmlNodeType_Text:
            if (FAILED(hr = reader->GetValue(&pwszValue, NULL))) {
                wprintf(L"Error getting value, error is %08.8lx", hr);
                return -1;
            }
            text = converter.to_bytes(pwszValue);
            addChildNode(parents, textTag, parent, nlohmann::json(text));
            break;

        case XmlNodeType_Whitespace:
            break;

        case XmlNodeType_CDATA:
            if (FAILED(hr = reader->GetValue(&pwszValue, NULL))) {
                wprintf(L"Error getting value, error is %08.8lx", hr);
                return -1;
            }
            text = converter.to_bytes(pwszValue);
            addChildNode(parents, textTag, parent, nlohmann::json(text));
            break;

        case XmlNodeType_ProcessingInstruction:
            if (FAILED(hr = reader->GetLocalName(&pwszLocalName, NULL))) {
                wprintf(L"Error getting name, error is %08.8lx", hr);
                return -1;
            }
            if (FAILED(hr = reader->GetValue(&pwszValue, NULL))) {
                wprintf(L"Error getting value, error is %08.8lx", hr);
                return -1;
            }
            //wprintf(L"Processing Instruction name:%S value:%S\n", pwszLocalName, pwszValue);
            break;

        case XmlNodeType_Comment:
            if (FAILED(hr = reader->GetValue(&pwszValue, NULL))) {
                wprintf(L"Error getting value, error is %08.8lx", hr);
                return -1;
            }
            text = converter.to_bytes(pwszValue);
            addChildNode(parents, commentTag, parent, nlohmann::json(text));
            break;

        case XmlNodeType_DocumentType:
            break;
        }
    }
    if (root != nullptr) {
        flatten(doc, rootName, *root);
    }
    return 0;

}

int main(int argc, char*argv[])
{
    for (int i = 1; i < argc; i++)
    {
        nlohmann::json doc;
        try {
            convert(argv[i], doc);
        }
        catch (std::exception& ex) {
            printf("Exception: %s\n", ex.what());
        }
        std::cout << doc;
    }
    return 0;
}

