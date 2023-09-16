using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class VoxelMap : MonoBehaviour
{
    public Material[] materials;

    int SizeMap = 16;
    int HeightMap = 256;

    public Chunk[,] chunks = new Chunk[64, 64];

    List<DataBlock> dataBlocks = new List<DataBlock>();


    private void Awake()
    {
        dataBlocks.Add(new DataBlock(new Vector2(2, 15), new Vector2(2, 15), new Vector2(2, 15), true));//dirt 0
        dataBlocks.Add(new DataBlock(new Vector2(1, 15), new Vector2(1, 15), new Vector2(1, 15), true));//stone 1
        dataBlocks.Add(new DataBlock(new Vector2(0, 15), new Vector2(3, 15), new Vector2(2, 15), true));//grass 2
        dataBlocks.Add(new DataBlock(new Vector2(4, 15), new Vector2(4, 15), new Vector2(4, 15), true));//plank 3
        dataBlocks.Add(new DataBlock(new Vector2(5, 14), new Vector2(4, 14), new Vector2(5, 14), true));//wood 4
        dataBlocks.Add(new DataBlock(new Vector2(5, 12), new Vector2(5, 12), new Vector2(5, 12), false, false));//leaf 5 
        dataBlocks.Add(new DataBlock(new Vector2(2, 14), new Vector2(2, 14), new Vector2(2, 14), true));//sand 6
        dataBlocks.Add(new DataBlock(new Vector2(2, 11), new Vector2(4, 11), new Vector2(2, 15), true));//snow 7
        dataBlocks.Add(new DataBlock(new Vector2(15, 3), new Vector2(15, 3), new Vector2(15, 3), false, true));//water 8


        for (int x = 0; x < 64; x++)
        {
            for (int z = 0; z < 64; z++)
            {
                chunks[x, z] = new Chunk();
            }
        }
    }

    public Chunk LoadChunk(Vector2Int pos)
    {
        return Init(pos.x, pos.y);
    }

    public Chunk ShowChunk(Vector2Int pos, Chunk[,] _chunks)
    {
        Chunk chunk = _chunks[pos.x, pos.y];
        chunk.meshMap = UpdateAllBlock(pos, _chunks, dataBlocks);

        return chunks[pos.x, pos.y];
    }

    public Chunk Init(int c_x, int c_z)
    {
        Chunk chunk = chunks[c_x, c_z];

        if (!chunk.isLoaded)
        {
            chunk.matriceBlock = new int[SizeMap, HeightMap, SizeMap];
            chunk.meshMap = new MeshBlock[SizeMap, HeightMap, SizeMap];

            for (int x = 0; x < SizeMap; x++)
            {
                for (int y = 0; y < HeightMap; y++)
                {
                    for (int z = 0; z < SizeMap; z++)
                    {
                        if(chunk.matriceBlock[x, y, z] == 0)
                        {
                            chunk.matriceBlock[x, y, z] = -1; // Initialisez chaque élément avec -1
                        }           
                    }
                }
            }
        }

        

        return chunk;
    }



    bool IsValid(Vector3Int pos)
    {
        bool valid = true;
        if (pos.x >= 1024 || pos.x < 0 ||
            pos.y >= 256 || pos.y < 0 ||
            pos.z >= 1024 || pos.z < 0)
            valid = false;
        return valid;
    }

    bool IsValidLocal(Vector3Int pos)
    {
        bool valid = true;
        if (pos.x >= 16 || pos.x < 0 ||
            pos.y >= 256 || pos.y < 0 ||
            pos.z >= 16 || pos.z < 0)
            valid = false;
        return valid;
    }

    public void NewBlock(Vector3Int pos, int id)
    {

        if (IsValid(pos))
        {
            Vector3Int posLocal = PosWorlkToChunk(pos);
            if (GetChunk(pos, chunks).matriceBlock[posLocal.x, posLocal.y, posLocal.z] == -1)
                GetChunk(pos, chunks).matriceBlock[posLocal.x, posLocal.y, posLocal.z] = id;
        }
    }

    public void ForceNewBlock(Vector3Int pos, int id)
    {

        if (IsValid(pos))
        {
            Vector3Int posLocal = PosWorlkToChunk(pos);
            GetChunk(pos, chunks).matriceBlock[posLocal.x, posLocal.y, posLocal.z] = id;
        }
    }

    public Chunk GetChunk(Vector3Int worldBlockPosition, Chunk[,] _chunks)
    {
        int chunkX = Mathf.FloorToInt(SafeDivision(worldBlockPosition.x, 16));
        int chunkZ = Mathf.FloorToInt(SafeDivision(worldBlockPosition.z, 16));

        return _chunks[chunkX, chunkZ];
    }

    public Vector2Int GetChunkPosition(Vector3Int worldBlockPosition)
    {
        int chunkX = Mathf.FloorToInt(SafeDivision(worldBlockPosition.x, 16));
        int chunkZ = Mathf.FloorToInt(SafeDivision(worldBlockPosition.z, 16));

        return new Vector2Int(chunkX, chunkZ);
    }


    Vector3Int PosWorlkToChunk(Vector3Int pos)
    {
        return new Vector3Int(pos.x % 16, pos.y, pos.z % 16);
    }

    public static int SafeDivision(int numerator, int denominator, int defaultValue = 0)
    {
        if (denominator == 0)
        {
            // Évitez la division par zéro en retournant une valeur par défaut
            return defaultValue;
        }

        return numerator / denominator;
    }

    public void AddBlock(Vector3Int _pos, int id)
    {
        if (IsValid(_pos))
        {
            Vector3Int posC = PosWorlkToChunk(_pos);
            if (GetChunk(_pos, chunks).matriceBlock[posC.x, posC.y, posC.z] == -1)
            {
                GetChunk(_pos, chunks).matriceBlock[posC.x, posC.y, posC.z] = id;
                //UpdateAllBlock();
                UpdateArrondBlock(_pos, id);
            }
        }

    }

    public void RemoveBlock(Vector3Int _pos)
    {
        Vector3Int lPos = PosWorlkToChunk(_pos);
        if (IsValid(_pos) && GetChunk(_pos, chunks).matriceBlock[lPos.x, lPos.y, lPos.z] != -1)
        {
            GetChunk(_pos, chunks).matriceBlock[lPos.x, lPos.y, lPos.z] = -1;
            //UpdateAllBlock();
            UpdateArrondBlock(_pos);
        }
    }

    void UpdateArrondBlock(Vector3Int _pos, int id = -1)
    {
        UpdateBlock(_pos, id);
        UpdateBlock(_pos + Vector3Int.forward);
        UpdateBlock(_pos + Vector3Int.back);
        UpdateBlock(_pos + Vector3Int.right);
        UpdateBlock(_pos + Vector3Int.left);
        UpdateBlock(_pos + Vector3Int.up);
        UpdateBlock(_pos + Vector3Int.down);
        Debug.Log("Chunk " + GetChunkPosition(_pos));

        GetChunk(_pos, chunks).ReMesh(materials);

        if (GetChunk(_pos, chunks) != GetChunk(_pos + Vector3Int.forward, chunks))
            GetChunk(_pos + Vector3Int.forward, chunks).ReMesh(materials);

        if (GetChunk(_pos, chunks) != GetChunk(_pos + Vector3Int.back, chunks))
            GetChunk(_pos + Vector3Int.back, chunks).ReMesh(materials);

        if (GetChunk(_pos, chunks) != GetChunk(_pos + Vector3Int.right, chunks))
            GetChunk(_pos + Vector3Int.right, chunks).ReMesh(materials);

        if (GetChunk(_pos, chunks) != GetChunk(_pos + Vector3Int.left, chunks))
            GetChunk(_pos + Vector3Int.left, chunks).ReMesh(materials);

    }

    int GetTypeBlock(Vector3Int _pos)
    {
        Vector3Int lPos = PosWorlkToChunk(_pos);
        return GetChunk(_pos, chunks).matriceBlock[lPos.x, lPos.y, lPos.z];
    }

    void UpdateBlock(Vector3Int _pos, int id = -1)
    {
        if (IsValid(_pos))
        {
            Vector3Int lPos = PosWorlkToChunk(_pos);

            Chunk chunk = GetChunk(_pos, chunks);

            chunk.meshMap[lPos.x, lPos.y, lPos.z] = new MeshBlock();
            if (id == -1)
            {
                if (chunk.matriceBlock[lPos.x, lPos.y, lPos.z] != -1)
                {
                    CreateMeshBlock(_pos, GetTypeBlock(_pos), chunks, dataBlocks, ref chunk.meshMap);
                    //CreateMeshBlock(_pos, GetTypeBlock(_pos)); ------------------------------------------------------------------------------- pour poseé ou cassé action requise, faudra ajoute le meshMap et call ReMesh
                }
            }
            else
            {
                CreateMeshBlock(_pos, id, chunks, dataBlocks, ref chunk.meshMap);
                //CreateMeshBlock(_pos, id);---------------------------------------------------------------------------------------------------- pour poseé ou cassé action requise, faudra ajoute le meshMap et call ReMesh
            }

        }
    }

    public void CreateMeshBlock(Vector3Int pos, int id, Chunk[,] _chunks, List<DataBlock> _dataBlock, ref MeshBlock[,,] meshBlock)
    {
        Vector2 texUp = dataBlocks[id].UpTexture;
        Vector2 texBorder = dataBlocks[id].BorderTexture;
        Vector2 texDown = dataBlocks[id].DownTexture;

        IfCreateFace(pos, Vector3Int.forward, texBorder, _chunks, _dataBlock, ref meshBlock);
        IfCreateFace(pos, Vector3Int.back, texBorder, _chunks, _dataBlock, ref meshBlock);
        IfCreateFace(pos, Vector3Int.right, texBorder, _chunks, _dataBlock, ref meshBlock);
        IfCreateFace(pos, Vector3Int.left, texBorder, _chunks, _dataBlock, ref meshBlock);
        IfCreateFace(pos, Vector3Int.up, texUp, _chunks, _dataBlock, ref meshBlock);
        IfCreateFace(pos, Vector3Int.down, texDown, _chunks, _dataBlock, ref meshBlock);


    }

    public Vector3Int TransformLocalToGlobal(Vector3Int localBlockPosition, Vector2Int chunkPosition)
    {
        // Calculez les coordonnées mondiales en ajoutant les coordonnées locales aux coordonnées du chunk
        int worldX = chunkPosition.x * 16 + localBlockPosition.x;
        int worldY = localBlockPosition.y;
        int worldZ = chunkPosition.y * 16 + localBlockPosition.z;

        return new Vector3Int(worldX, worldY, worldZ);
    }

    public MeshBlock[,,] UpdateAllBlock(Vector2Int pos, Chunk[,] _chunks, List<DataBlock> _dataBlock)
    {
        MeshBlock[,,] meshMap = new MeshBlock[16, 256, 16];
        int[,,] matriceBlock = _chunks[pos.x, pos.y].matriceBlock;

        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 256; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    meshMap[x, y, z] = new MeshBlock();
                    if (matriceBlock[x, y, z] != -1)
                    {
                        /*GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.transform.position = new Vector3Int(x, y, z)+ chunk.cube.transform.position;*/
                        Vector3Int posChunkObject = new Vector3Int(pos.x * 16, 0, pos.y * 16);
                        CreateMeshBlock(new Vector3Int(x, y, z) + posChunkObject, matriceBlock[x, y, z], _chunks, _dataBlock, ref meshMap);
                    }
                }
            }
        }

        //ReMesh(chunk, pos);
        //ReMesh();
        return meshMap;
    }





    void IfCreateFace(Vector3Int pos, Vector3Int direction, Vector2 tex, Chunk[,] _chunks, List<DataBlock> _dataBlocks, ref MeshBlock[,,] meshMap)
    {
        Vector3Int posCTest = PosWorlkToChunk(pos + direction);
        Vector3Int posCMine = PosWorlkToChunk(pos);
        Chunk chunkTest = GetChunk(pos + direction, _chunks);
        Chunk chunkMine = GetChunk(pos, _chunks);

        if (IsValidLocal(posCTest) && IsValidLocal(posCMine))
        {
            int typeBockTest = chunkTest.matriceBlock[posCTest.x, posCTest.y, posCTest.z];
            int typeBockMine = chunkMine.matriceBlock[posCMine.x, posCMine.y, posCMine.z];

            if (typeBockTest == -1 || 
                (_dataBlocks[typeBockMine].Solid && !_dataBlocks[typeBockTest].Solid))
            {
                meshMap[posCMine.x, posCMine.y, posCMine.z].IsTransparent = _dataBlocks[typeBockMine].Transparent;
                CreateFace(posCMine, direction, tex, ref meshMap);
            }
                
        }
    }

    void CreateFace(Vector3Int pos, Vector3Int direction, Vector2 tex, ref MeshBlock[,,] meshMap)
    {
        if(direction == Vector3Int.forward)
            CreateFaceForward(pos, tex, ref meshMap);

        if (direction == Vector3Int.back)
            CreateFaceBack(pos, tex, ref meshMap);

        if (direction == Vector3Int.right)
            CreateFaceRight(pos, tex, ref meshMap);

        if (direction == Vector3Int.left)
            CreateFaceLeft(pos, tex, ref meshMap);

        if (direction == Vector3Int.up)
            CreateFaceUp(pos, tex, ref meshMap);

        if (direction == Vector3Int.down)
            CreateFaceDown(pos, tex, ref meshMap);
    }

    void CreateFaceBack(Vector3Int pos, Vector2 texture, ref MeshBlock[,,] meshMap)
    {
        Vector3[] _vertices = {
            // Face avant
            new Vector3(pos.x, pos.y, pos.z),
            new Vector3(pos.x+1, pos.y, pos.z),
            new Vector3(pos.x+1, pos.y+1, pos.z),
            new Vector3(pos.x, pos.y+1, pos.z),
        };

        // Définissez les triangles pour le cube.
        int[] _triangles = {
            // Face avant
            0, 2, 1,
            0, 3, 2,
        };

        Vector2[] _uv = {
            // Face avant
            texture + new Vector2(0, 0),
            texture + new Vector2(1, 0),
            texture + new Vector2(1, 1),
            texture + new Vector2(0, 1),
        };

        Vector3[] _normals = {
            // Normales pour la face avant
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back,
        };

        meshMap[pos.x, pos.y, pos.z].meshFaces[(int)EnumDirection.Back] = new MeshFace(_vertices, _triangles, _uv, _normals);
    }

    void CreateFaceForward(Vector3Int pos, Vector2 texture, ref MeshBlock[,,] meshMap)
    {
        Vector3[] _vertices = {
            // Face arrière
            new Vector3(pos.x, pos.y, pos.z+1),
            new Vector3(pos.x+1, pos.y, pos.z+1),
            new Vector3(pos.x+1, pos.y+1, pos.z+1),
            new Vector3(pos.x, pos.y+1, pos.z+1),
        };

        // Définissez les triangles pour le cube.
        int[] _triangles = {
            // Face arrière
            0, 1, 2,
            0, 2, 3,
        };

        Vector2[] _uv = {
            // Face avant
            texture + new Vector2(0, 0),
            texture + new Vector2(1, 0),
            texture + new Vector2(1, 1),
            texture + new Vector2(0, 1),
        };

        Vector3[] _normals = {
            // Normales pour la face arrière (inversées)
            Vector3.forward,
            Vector3.forward,
            Vector3.forward,
            Vector3.forward,
        };

        meshMap[pos.x, pos.y, pos.z].meshFaces[(int)EnumDirection.Forward] = new MeshFace(_vertices, _triangles, _uv, _normals);
    }

    void CreateFaceRight(Vector3Int pos, Vector2 texture, ref MeshBlock[,,] meshMap)
    {
        Vector3[] _vertices = {
            // Côté droit
            new Vector3(pos.x+1, pos.y, pos.z),
            new Vector3(pos.x+1, pos.y, pos.z+1),
            new Vector3(pos.x+1, pos.y+1, pos.z+1),
            new Vector3(pos.x+1, pos.y+1, pos.z),
        };

        // Définissez les triangles pour le cube.
        int[] _triangles = {
            // Côté droit
            0, 2, 1,
            0, 3, 2,
        };

        Vector2[] _uv = {
            // Face avant
            texture + new Vector2(0, 0),
            texture + new Vector2(1, 0),
            texture + new Vector2(1, 1),
            texture + new Vector2(0, 1),
        };

        Vector3[] _normals = {
            // Normales pour le côté droit
            Vector3.right,
            Vector3.right,
            Vector3.right,
            Vector3.right,
        };


        meshMap[pos.x, pos.y, pos.z].meshFaces[(int)EnumDirection.Right] = new MeshFace(_vertices, _triangles, _uv, _normals);
    }

    void CreateFaceLeft(Vector3Int pos, Vector2 texture, ref MeshBlock[,,] meshMap)
    {
        Vector3[] _vertices = {
            // Côté gauche
            new Vector3(pos.x, pos.y, pos.z),
            new Vector3(pos.x, pos.y, pos.z+1),
            new Vector3(pos.x, pos.y+1, pos.z+1),
            new Vector3(pos.x, pos.y+1, pos.z),
        };

        // Définissez les triangles pour le cube.
        int[] _triangles = {
            // Côté gauche
            0, 1, 2,
            0, 2, 3,
        };

        Vector2[] _uv = {
            // Face avant
            texture + new Vector2(0, 0),
            texture + new Vector2(1, 0),
            texture + new Vector2(1, 1),
            texture + new Vector2(0, 1),
        };

        Vector3[] _normals = {
            // Normales pour le côté gauche (inversées)
            Vector3.left,
            Vector3.left,
            Vector3.left,
            Vector3.left,
        };

        meshMap[pos.x, pos.y, pos.z].meshFaces[(int)EnumDirection.Left] = new MeshFace(_vertices, _triangles, _uv, _normals);
    }

    void CreateFaceUp(Vector3Int pos, Vector2 texture, ref MeshBlock[,,] meshMap)
    {
        Vector3[] _vertices = {
            // Dessus
            new Vector3(pos.x, pos.y+1, pos.z),
            new Vector3(pos.x+1, pos.y+1, pos.z),
            new Vector3(pos.x+1, pos.y+1, pos.z+1),
            new Vector3(pos.x, pos.y+1, pos.z+1),
        };

        // Définissez les triangles pour le cube.
        int[] _triangles = {
            // Dessus
            0, 2, 1,
            0, 3, 2,
        };

        Vector2[] _uv = {
            // Face avant
            texture + new Vector2(0, 0),
            texture + new Vector2(1, 0),
            texture + new Vector2(1, 1),
            texture + new Vector2(0, 1),
        };

        Vector3[] _normals = {
            // Normales pour le dessus
            Vector3.up,
            Vector3.up,
            Vector3.up,
            Vector3.up,
        };


        
        meshMap[pos.x, pos.y, pos.z].meshFaces[(int)EnumDirection.Up] = new MeshFace(_vertices, _triangles, _uv, _normals);
    }

    void CreateFaceDown(Vector3Int pos, Vector2 texture, ref MeshBlock[,,] meshMap)
    {
        Vector3[] _vertices = {
            // Dessous
            new Vector3(pos.x, pos.y, pos.z),
            new Vector3(pos.x+1, pos.y, pos.z),
            new Vector3(pos.x+1, pos.y, pos.z+1),
            new Vector3(pos.x, pos.y, pos.z+1)
        };

        // Définissez les triangles pour le cube.
        int[] _triangles = {
            // Dessous
            0, 1, 2,
            0, 2, 3
        };

        Vector2[] _uv = {
            // Face avant
            texture + new Vector2(0, 0),
            texture + new Vector2(1, 0),
            texture + new Vector2(1, 1),
            texture + new Vector2(0, 1),
        };

        Vector3[] _normals = {
            // Normales pour le dessous (inversées)
            Vector3.down,
            Vector3.down,
            Vector3.down,
            Vector3.down
        };
        meshMap[pos.x, pos.y, pos.z].meshFaces[(int)EnumDirection.Down] = new MeshFace(_vertices, _triangles, _uv, _normals);
    }


}

[System.Serializable]
public class MeshBlock
{
    public MeshFace[] meshFaces;
    public bool IsTransparent;

    public MeshBlock()
    {
        meshFaces = new MeshFace[6];
    }
}

[System.Serializable]
public class MeshFace
{

    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();
    public List<Vector2> uv = new List<Vector2>();
    public List<Vector3> normals = new List<Vector3>();

    public MeshFace(Vector3[] vertices, int[] triangles, Vector2[] uv, Vector3[] normals)
    {
        this.vertices = vertices.ToList();
        this.triangles = triangles.ToList();
        this.uv = uv.ToList();
        this.normals = normals.ToList();
    }
}

[System.Serializable]
public class Chunk
{
    public Mesh cubeMesh;
    public int[,,] matriceBlock;
    public bool isLoaded;
    public MeshBlock[,,] meshMap;
    public GameObject cube;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    public Chunk()
    {
        matriceBlock = new int[16, 256, 16];
        meshMap = new MeshBlock[16, 256, 16];
    }

    public void ReMesh(Material[] materials)
    {

        if(meshFilter == null)
        {
            meshFilter = cube.AddComponent<MeshFilter>();
        }      
        if (meshRenderer == null)
            meshRenderer = cube.AddComponent<MeshRenderer>();
        if (meshCollider == null)
            meshCollider = cube.AddComponent<MeshCollider>();
        cubeMesh = meshFilter.mesh;
        cubeMesh.subMeshCount = 2;



        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<int> trianglesTransparent = new List<int>();
        List<int> trianglesNotTransparent = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        int nV = 0;

        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 256; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int d = 0; d < 6; d++)
                    {

                        if (this.meshMap[x, y, z] != null && this.meshMap[x, y, z].meshFaces[d] != null)
                        {
                            vertices.AddRange(this.meshMap[x, y, z].meshFaces[d].vertices);
                            List<int> tempTriangles = new List<int>(this.meshMap[x, y, z].meshFaces[d].triangles);


                            for (int i = 0; i < tempTriangles.Count; i++)
                            {
                                tempTriangles[i] += nV;
                            }

                            triangles.AddRange(tempTriangles);
                            uv.AddRange(this.meshMap[x, y, z].meshFaces[d].uv);

                            if (this.meshMap[x, y, z].IsTransparent)
                                trianglesTransparent.AddRange(tempTriangles.ToArray());
                            else
                                trianglesNotTransparent.AddRange(tempTriangles.ToArray());

                            normals.AddRange(this.meshMap[x, y, z].meshFaces[d].normals);
                            nV += 4;
                        }
                    }
                }
            }
        }
        this.cubeMesh.Clear();

        this.cubeMesh.vertices = vertices.ToArray();

        this.cubeMesh.subMeshCount = 2;
        this.cubeMesh.SetTriangles(trianglesNotTransparent, 0);
        this.cubeMesh.SetTriangles(trianglesTransparent, 1);
        //mythis.cubeMesh.triangles = triangles.ToArray();

        this.cubeMesh.uv = uv.ToArray();
        this.cubeMesh.normals = normals.ToArray();

        this.meshFilter.mesh = this.cubeMesh;
        this.meshCollider.sharedMesh = this.cubeMesh;
        this.meshRenderer.materials = materials;
    }
}

public class DataBlock
{
    public Vector2 UpTexture;
    public Vector2 BorderTexture;
    public Vector2 DownTexture;
    public bool Solid;
    public bool Transparent;

    public DataBlock(Vector2 texUp, Vector2 texBorder, Vector2 texDown, bool _solid = true, bool transparnet = false)
    {
        UpTexture = texUp;
        BorderTexture = texBorder;
        DownTexture = texDown;
        Solid = _solid;
        Transparent = transparnet;
    }
}

public enum EnumDirection
{
    Forward,
    Back, 
    Right,
    Left,
    Up, 
    Down
}