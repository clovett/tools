# Readme

This is a .NET console app that runs a w3c test suite from [https://www.w3.org/XML/Test/](https://www.w3.org/XML/Test)
testing the `System.Xml.XmlReader` using those data driven tests by loading each test into an `System.Xml.XmlDocument` 
object and reporting the results.

Usage:

```
cd bin\debug\net6.0
TestDriver.exe https://www.w3.org/XML/Test/xmlts20031210.zip
```

It reports all test failures, and the reson for the failure along with a summary of results, which
on .NET 6.0 is as follows:

```
1026 tests run
191 tests failed
835 tests passed
pass rate 81 %
```

