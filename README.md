# Open76

Open76 is an engine reimplementation of Activision's Interstate '76 on the Unity platform.

## Why?
For fun. Tinkering with Interstate '76 started many years ago, but I never made much progress on the monstrous number of file formats presented. Until early this year when I discovered that blogger "That Tony" reverse engineered many of the formats on his blog http://hackingonspace.blogspot.se. Some formats had been reversed years ago by modders, although these specifications were hard to find since most of the sites were long gone (bless web-archive!).

In my opinion, Interstate '76 is one of the best games ever made. It suffers from an aging engine though, and in recent years it has become increasingly harder to play it on modern platforms.
The primary goal of this project is to provide the same playing experience in a modern engine while conforming to the original data file specifications.
A long term goal is to extend the engine with various modern features, such as HMD and VR support.

## What works?
* Parsing of mission files (*.msn), parsing and rendering of sky, terrains (*.ter), roads and the objects therein (*.scf).
* Parsing and rendering of cars (*.vcf, *.vdf)
* Parsing of texture files (*.map, *.vqm).

Some features of the above are not yet fully implemented, see Issues.

## What needs to be done?
A lot of things! Take a look at the issues list.

## How do I contribute?
Fork this repository. Look in the Issues list. Communicate that you're committing to fixing an issue and finally submit a pull request.

## License
Licensed under the GPL version 3.

This is in no way affiliated with or endorsed by Activision or any other company.