using Assimp;
using vmodel;
using StbImageSharp;
public static class FileImports
{
    static AssimpContext _context = MakeContext();
    private static AssimpContext MakeContext()
    {
        AssimpContext c = new AssimpContext();
        return c;
    }
    public static VModel? LoadModelWithAssimp(string path, out Exception? error, ImageResult fallbackTexture)
    {
        try
        {
            Scene s = _context.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals | PostProcessSteps.FlipUVs);
            //We need to get the data out of Assimp into the format that VModel uses.
            // First, we need to figure out the vertex attributes
            if(s.MeshCount != 1)throw new Exception("Only objects with one mesh are supported");
            if(s.TextureCount > 1) throw new Exception("Only objects with 1 texture are supported");
            List<EAttribute> attributes = new List<EAttribute>();
            Mesh m = s.Meshes[0];
            attributes.Add(EAttribute.position); //position
            if(m.HasTextureCoords(0))attributes.Add(EAttribute.textureCoords); //tex coords - only channel 0 is actually used
            if(m.HasNormals)attributes.Add(EAttribute.normal); //normals
            if(m.HasVertexColors(0))attributes.Add(EAttribute.rgbaColor); //vertex colors
            //Now that we have the attributes, we can load in the data itself
            int totalAttributes = 0;
            for(int i=0; i<attributes.Count; i++){
                totalAttributes += ((int)attributes[i] % 5);
            }
            float[] vertices = new float[(int)(m.VertexCount * totalAttributes)];
            int idx = 0;
            //load the mesh data into the vertices
            for(int v=0; v<m.VertexCount; v++)
            {
                vertices[idx++] = (m.Vertices[v].X);
                vertices[idx++] = (m.Vertices[v].Y);
                vertices[idx++] = (m.Vertices[v].Z);
                if(m.HasTextureCoords(0))
                {
                    vertices[idx++] = (m.TextureCoordinateChannels[0][v].X);
                    vertices[idx++] = (m.TextureCoordinateChannels[0][v].Y);
                }
                if(m.HasNormals)
                {
                    vertices[idx++] = (m.Normals[v].X);
                    vertices[idx++] = (m.Normals[v].Y);
                    vertices[idx++] = (m.Normals[v].Z);
                }
                if(m.HasVertexColors(0))
                {
                    vertices[idx++] = (m.VertexColorChannels[0][v].R);
                    vertices[idx++] = (m.VertexColorChannels[0][v].G);
                    vertices[idx++] = (m.VertexColorChannels[0][v].B);
                    vertices[idx++] = (m.VertexColorChannels[0][v].A);
                }
            }
            //Now we have the vertices, but we need the indices too.
            uint[] indices = m.GetUnsignedIndices();
            VMesh mesh = new VMesh(vertices.ToArray(), indices, attributes.ToArray(), null);
            //Finally we need the texture, if there is one.
            // We only get the first texture - remeber we only support models with a single texture.
            ImageResult texture;
            if(s.HasTextures)
            {
                var textureRaw = s.Textures[0];
                if(textureRaw.IsCompressed)
                {
                    //use StbImage to load the compressed image
                    texture = ImageResult.FromMemory(textureRaw.CompressedData, ColorComponents.RedGreenBlueAlpha);
                }
                else{
                    texture = new ImageResult();
                    //get and marshall texture data
                    Texel[] texels = textureRaw.NonCompressedData;
                    byte[] data = texture.Data = new byte[texels.Length*4];
                    for(int t=0; t<texels.Length; t++)
                    {
                        Texel texel = texels[t];
                        data[t*4+0] = texel.R;
                        data[t*4+0] = texel.G;
                        data[t*4+0] = texel.B;
                        data[t*4+0] = texel.A;
                    }
                    //image properties
                    texture.Comp = ColorComponents.RedGreenBlueAlpha;
                    texture.Width = textureRaw.Width;
                    texture.Height = textureRaw.Height;
                }
            }
            else
            {
                texture = fallbackTexture;
            }
            error = null;
            return new VModel(
                mesh, texture, null
            );
        } catch (Exception e)
        {
            error = e;
            return null;
        }
    }
}