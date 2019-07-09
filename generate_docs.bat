@echo off

mkdir docs

cd "SecureTextEditor.Crypto"
doxygen .doxygen
cd..

cd "SecureTextEditor.File"
doxygen .doxygen
cd..

cd "SecureTextEditor.GUI"
doxygen .doxygen
cd..