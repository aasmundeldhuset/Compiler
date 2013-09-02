rem Usage: CompileToJasmin.cmd primes.vsl VSL.j
rem Running the result: java VslMain
bin\Debug\Compiler.exe jasmin "%1" "%2"
java -jar c:\Applications\Development\Jasmin\jasmin.jar "%2"
