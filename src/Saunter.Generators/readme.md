# Generate code based on AsyncApi annoted interfaces

## Usage
Mark a class with the ```[AsyncApiService]``` attribute to generate an implemenration of the provided AsyncApi interface(s).
```
[AsyncApiService("ReceiverTemplate.txt", typeof(IAbcCommands), typeof(IXyzCommands))]
internal partial class CommandsListener : IMoreCommands;
```