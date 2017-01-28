rem Usage: CompileToX86.cmd primes.vsl VSL
bin\Debug\Compiler.exe x86 "%1" "%2".s
nasm -fwin32 "%2".s
link /entry:main /subsystem:console /nodefaultlib /out:"%2".exe "%2".obj "C:\Program Files (x86)\Windows Kits\10\Lib\10.0.14393.0\um\x86\kernel32.Lib"