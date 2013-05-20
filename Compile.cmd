bin\Debug\Compiler.exe "%1" "%2"
ilasm /exe "%2" /deb=opt
peverify /md /il "%3"
