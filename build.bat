@echo off
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /out:"%~dp0MicKnob.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll "%~dp0MicKnob.cs"
