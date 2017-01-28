; https://en.wikibooks.org/wiki/X86_Assembly/NASM_Syntax

global _main

extern _GetStdHandle@4
extern _ReadConsoleA@20
extern _WriteConsoleA@20
extern _ExitProcess@4

%define STDOUT_HANDLE_PARAM -11
%define STDIN_HANDLE_PARAM -10
%define BUFFER_SIZE 256
%define MAX_STRING_LEN 255

section .data
	str:	db 'Hello world!',0x0d,0x0a
	strLen:	equ $-str

section .bss
	numCharsRead:		resd 1
	numCharsWritten:	resd 1
	stdInHandle:		resd 1
	stdOutHandle:		resd 1
	buffer:				resb BUFFER_SIZE

section .text

_main:
	push	dword STDIN_HANDLE_PARAM
	call	_GetStdHandle@4
	mov		[stdInHandle], eax

	push	dword STDOUT_HANDLE_PARAM
	call	_GetStdHandle@4
	mov		[stdOutHandle], eax

	push	dword 0
	push	numCharsRead
	push	dword MAX_STRING_LEN
	push	buffer
	push	dword [stdInHandle]
	call	_ReadConsoleA@20

	push	dword 0
	push	numCharsWritten
	push	dword [numCharsRead]
	push	buffer
	push	dword [stdOutHandle]
	call	_WriteConsoleA@20

;	push	dword 0
;	push	numCharsWritten
;	push	dword strLen
;	push	str
;	push	dword [stdOutHandle]
;	call	_WriteConsoleA@20

	push	dword 0
	call	_ExitProcess@4
