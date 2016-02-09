# 53230A-Toolkit
A collection of simple command line utilities to communicate with and program the Keysight 53210/53220/53230A frequency counters. The utilities use only TCP/IP sockets, and has no dependancies to NI or Keysight drivers. This also means they will not work over USB or GPIB. Familiarity with the 53230a programming guide is required.

## Read
Issues ":Initiate:Immediate", followed by repeated "Read?". Runs untill aborted with ctrl-c.

## R [n]
n - Optional, number of samples to request per IO. Default 1. For fast measurements (> 10 measurements per second), increase this number. Note that this does not configure the counter to make fast measurements, it only instructs "R" to request more than one sample per IO. Runs untill aborted with ctrl-c.

Issues ":Initiate:Immediate", followed by repeated "DATA:REMove <n>,WAIT". This can be used to retrieve continous gap-free measurements on the 53230A. Note that ":Sample:count" must be set to the total number og samples to retrieve over the measurement session. See "Learn".

## Learn [\<statements\>]
\<statements\> - Optional configuration statements to send to the counter, e.g. ":SAMPle:COUNt 1e6". If no statements are given, lists the current non-default configuration settings of the counter.

This is particularly useful to ensure the counter is configured the same way for separate measurement sessions. Before the first measurement session begins, configure the counter according to the measurement to be taken, then save the configuration with "Learn > settings.txt". When the same measurements are to be repeated at a later stage, the exact same settings can be restored using "learn < settings.txt".
