# Readme

This is a .NET console app that runs a w3c test suite from [https://www.w3.org/XML/Test/](https://www.w3.org/XML/Test)
testing the `System.Xml.XmlReader` using those data driven tests by loading each test into an `System.Xml.XmlDocument` 
object and reporting the results.

Usage:

```
cd bin\debug\net6.0
TestDriver.exe https://www.w3.org/XML/Test/xmlts20031210.zip
```

It reports all test failures, and the reason for the failure along with a summary of results, which
on .NET 6.0 is as follows:

| Suite                                         | Tests  | Pass Rate    |
|-----------------------------------------------|--------|--------------|
| https://www.w3.org/XML/Test/xmlts20031210.zip | 1026   | 81 %         |
| https://www.w3.org/XML/Test/xmlts20080205.zip | 2559   | 76 %         |
| https://www.w3.org/XML/Test/xmlts20080827.zip | 2570   | 76 %         |
| https://www.w3.org/XML/Test/xmlts20130923.zip | 2585   | 76 %         |

 
