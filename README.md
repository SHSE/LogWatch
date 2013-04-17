LogWatch
========

Minimalistic viewer for NLog, Log4Net and others.

[
<img src="http://dabuttonfactory.com/b.png?t=Install%20via%20ClickOnce&f=Calibri&ts=24&tc=ffffff&tshs=1&tshc=222222&it=png&c=5&bgt=gradient&bgc=707070&ebgc=5c5c5c&hp=20&vp=11" />
](http://master.dl.sourceforge.net/project/logwatch-dotnet/LogWatch.application)

<img src="http://i.imgur.com/JMouXWK.png" width="600px" />

LogWatch allows you to view output from NLog and Log4Net loggers. 

## Features
* Designed to handle large log files (hundreds of megabytes, it doesn't read all the data into memory)
* Live file streaming support (view files that are currently being updated by the logger)
* Accepts log files in CSV or Log4J XML formats
* Automatic log format selection
* Receives log records via network
* Provides quick jump to a next Trace/Debug/Info/Warn/Error/Fatal record functionality
* Search using simple text search or regular expressions (press Ctrol+F to activate)

## File source

NLog configuration for CSV format:
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

## Network source

LogWatch accepts log records via UDP protocol in Log4J XML format (the same format as NLogViewer).

NLog configuration:
```xml
<target name="viewer" xsi:type="NLogViewer" address="udp://127.0.0.1:13370" includeNLogData="true">
  <parameter name="exception" layout="${exception:format=ToString}"/>
</target>
```

## TODO

- [ ] custom log record properties
- [ ] user provided parser using [Lex](http://dinosaur.compilertools.net/lex) syntax