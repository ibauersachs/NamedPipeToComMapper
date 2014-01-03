NamedPipeToComMapper
====================

NamedPipeToComMapper is a very simple service to map a named pipe to a physical COM port, e.g. to forward it to a Hyper-V VM.

Installation
------------
`Maklerzentrum.NamedPipeToComMapper.exe -install`

See the the [Topshelf](http://docs.topshelf-project.com/en/latest/overview/commandline.html) documentation for more command line options.

Configuration
-------------
All settings in `Maklerzentrum.NamedPipeToComMapper.exe.config` that start with `Connection` are treated as mappings. The format is:

`pipe-name|COM-Port,Baud-Rate,Parity,Data-Bits,Stop-Bits`

License
-------
BSD 2-Clause
