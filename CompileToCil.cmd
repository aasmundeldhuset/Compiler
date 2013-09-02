rem Usage: CompileToCil.cmd primes.vsl VSL.il
rem Running the result: VSL.exe
bin\Debug\Compiler.exe cil "%1" "%2"
ilasm /exe "%2" /deb=opt
peverify /md /il VSL.exe
