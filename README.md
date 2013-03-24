LogWatch
========

Minimalistic viewer for NLog and Log4Net

![LogWatch screenshot](http://i.imgur.com/1lSe8hK.png)

LogWatch allows you to view output from NLog and Log4Net loggers. 
* Designed to handle large log files (hundreds of megabytes)
* Accepts log files in CSV or Log4JXml formats
* Receives log records via network

# File source

NLog:
```xml
<target 
  name="file" 
  xsi:type="File" 
  encoding="utf-8"
  fileName="${specialfolder:folder=ApplicationData}\Forensics\Logs\Worker-${shortdate}.log">
  <layout xsi:type="CsvLayout">
    <column name="time" layout="${longdate}" />
    <column name="message" layout="${message}" />
    <column name="logger" layout="${logger}" />
    <column name="level" layout="${level}" />
    <column name="exception" layout="${exception:format=ToString}" />
  </layout>
</target>
```

# Network source

```xml
<target name="viewer" xsi:type="NLogViewer" address="udp://127.0.0.1:13370" includeNLogData="true">
  <parameter name="exception" layout="${exception:format=ToString}"/>
</target>
```

