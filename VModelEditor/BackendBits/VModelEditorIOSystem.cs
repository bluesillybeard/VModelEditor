using Assimp;
//This class is a workaround because libdl.so doesn't exist in my system (and I assume might not exist on others either!)
public sealed class VModelEditorIOSystem : IOSystem
{
    public override IOStream OpenFile(string pathToFile, FileIOMode fileMode)
    {
        System.Console.WriteLine("dsj;kalfhgsdghsdDSHAJFhjkas");
        throw new Exception("wut");
        
    }
}