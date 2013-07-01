LogWatch [<img src="http://teamcity.codebetter.com/app/rest/builds/buildType:(id:bt983)/statusIcon" />](http://teamcity.codebetter.com/viewType.html?buildTypeId=bt983)
========

Minimalistic viewer for NLog, Log4Net and others.

[
<img src="http://dabuttonfactory.com/b.png?t=Install%20via%20ClickOnce&f=Calibri&ts=24&tc=ffffff&tshs=1&tshc=222222&it=png&c=5&bgt=gradient&bgc=707070&ebgc=5c5c5c&hp=20&vp=11" />
](https://shse-distrib.s3-external-3.amazonaws.com/LogWatch/LogWatch.application) or  [download binaries](http://teamcity.codebetter.com/guestAuth/repository/downloadAll/bt983/.lastSuccessful/artifacts.zip)

<img src="http://i.imgur.com/JMouXWK.png" width="600px" />

LogWatch allows you to view output from NLog and Log4Net loggers. 

## Features
* Designed to handle large log files (hundreds of megabytes, it doesn't read all the data into memory)
* Live file streaming support (view files that are currently being updated by the logger)
* Built-in support for CSV and Log4J XML formats
* Automatic log format selection
* Receives log records via network
* Provides quick jump to a next Trace/Debug/Info/Warn/Error/Fatal record functionality
* Search using simple text search or regular expressions (press Ctrol+F to activate)
* Ability to define custom log parser using [Lex](http://dinosaur.compilertools.net/lex) syntax

## Sources

LogWatch has built-in support for the following log sources:
* File source
* UDP source

When using file source LogWatch sets a monitor on the file and provides live updates on the records.

## Formats

The application supports the following log formats out of the box:
* CSV format (with some limitations)
* Log4J XML format
* [Custom parser (defined using Lex syntax)](//github.com/SHSE/LogWatch/wiki/Lex-Parser)

LogWatch automatically determines format of the source.

CSV delimeter can be `;` or `,`. The log file must have header with some of the following feillds (case sensitive):
```
time
message
logger
level
exception
```

Here is an example configuration of CSV layout for NLog:
```xml
<target name="file" xsi:type="File" encoding="utf-8" fileName="mylog.log">
  <layout xsi:type="CsvLayout">
    <column name="time" layout="${longdate}" />
    <column name="message" layout="${message}" />
    <column name="logger" layout="${logger}" />
    <column name="level" layout="${level}" />
    <column name="exception" layout="${exception:format=ToString}" />
  </layout>
</target>
```

Log4J XML is the default format for NLogViewer. Here is an example configuration for NLog:
```xml
<target name="viewer" xsi:type="NLogViewer" address="udp://127.0.0.1:13370" includeNLogData="true">
  <parameter name="exception" layout="${exception:format=ToString}"/>
</target>
```
`NLogViewer` sends records via UDP in Log4J XML format.


## TODO

- [ ] documentation
- [ ] custom log record properties
