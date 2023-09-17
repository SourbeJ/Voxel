using System.Threading;
using System;
using UnityEngine;
using System.Collections.Generic;

public class VoxelMapGenerator : MonoBehaviour
{
    public VoxelMap voxelMap; // Le script du premier objet contenant le MeshFilter
    public int mapSize = 16; // Taille du terrain
    //public int mapHeight = 256; // Taille du terrain
    //public int gridSize = 200; // Taille du terrain

    [Header("perlin noise")]
    public int heightMap = 30; // Taille du terrain
    public float heightScale = 20.0f; // Échelle de hauteur pour le terrain
    public int octaves = 6; // Niveau d'octaves pour le bruit Perlin
    public float persistence = 0.3f; // Persistance pour le bruit Perlin (diminuée)
    public float zoom = 1;

    [Header("biome")]
    public int levelSnow = 70;
    public int levelSee = 40;

    [Header("grotte")]

    public float tunnelSpawnProbability = 0.05f;
    public int tunnelMinSize = 5;
    public int tunnelMaxSize = 15;
    public float tunnelRadius = 2.0f; // Rayon du tunnel principal
    public float tunnelNoise = 0f; // Amplitude du bruit pour rendre les tunnels plus naturels

    public float treeSpawnProbability = 0.05f; // Probabilité d'apparition d'un arbre par bloc d'herbe
    //Liste d'attente de chargement des chunkd
    Queue<MapThreadInfo<Chunk>> mapdataInfoQueue = new Queue<MapThreadInfo<Chunk>>();

    void Start()
    {
        /*GenerateChunk(new Vector2Int(0, 0));
        GenerateChunk(new Vector2Int(1, 0));
        GenerateChunk(new Vector2Int(0, 1));
        GenerateChunk(new Vector2Int(1, 1));
        GenerateChunk(new Vector2Int(2, 0));
        GenerateChunk(new Vector2Int(2, 1));*/
    }

    private void Update()
    {
        if (mapdataInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapdataInfoQueue.Count; i++)
            {
                MapThreadInfo<Chunk> threadInfo = mapdataInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    public Vector3Int TransformLocalToGlobal(Vector3Int localBlockPosition, Vector2Int chunkPosition)
    {
        // Calculez les coordonnées mondiales en ajoutant les coordonnées locales aux coordonnées du chunk
        int worldX = chunkPosition.x * 16 + localBlockPosition.x;
        int worldY = localBlockPosition.y;
        int worldZ = chunkPosition.y * 16 + localBlockPosition.z;

        return new Vector3Int(worldX, worldY, worldZ);
    }

    public Chunk GenerateChunk(Vector2Int chunkPos)
    {
        //Génetation du terrain des arbres des ocean/lac, et des montagnes
        Vector2 chunkOffset = new Vector2(chunkPos.x * 16, chunkPos.y * 16);

        Chunk chunk = voxelMap.chunks[chunkPos.x, chunkPos.y];

        if (!chunk.isLoaded)
        {
            voxelMap.LoadChunk(new Vector2Int(chunkPos.x, chunkPos.y));
            chunk.isLoaded = true;
            for (int x = 0; x < mapSize; x++)
            {
                for (int z = 0; z < mapSize; z++)
                {
                    // Utilise le bruit Perlin pour générer des hauteurs réalistes
                    float xCoord = x + chunkOffset.x;
                    float zCoord = z + chunkOffset.y;
                    float height = GeneratePerlinNoise2D(xCoord, zCoord, zoom, octaves, persistence, heightScale) + heightMap;

                    // Arrondir la position pour placer les blocs à des positions entières
                    Vector3Int blockPosition = new Vector3Int(Mathf.RoundToInt(x), Mathf.RoundToInt(height), Mathf.RoundToInt(z));
                    blockPosition = TransformLocalToGlobal(blockPosition, chunkPos);

                    if (UnityEngine.Random.value < tunnelSpawnProbability)
                    {
                        //GenerateTunnelNetwork(blockPosition);
                    }




                    // Placer des blocs d'herbe
                    if (blockPosition.y == Mathf.Round(height))
                    {

                        if (height < levelSee)
                        {
                            voxelMap.NewBlock(blockPosition, 6);
                            int hauteurRestanteWater = levelSee - blockPosition.y;
                            for (int i = 1; i < hauteurRestanteWater; i++)
                            {
                                Vector3Int posWater = blockPosition + Vector3Int.up * i;
                                voxelMap.NewBlock(posWater, 8);
                            }
                        }
                        else if (height > levelSnow)
                        {
                            voxelMap.NewBlock(blockPosition, 7);
                        }
                        else
                        {
                            voxelMap.NewBlock(blockPosition, 2);
                        }


                        // Ajouter 3 blocs de terre en dessous
                        for (int i = 1; i <= 2; i++)
                        {
                            Vector3Int dirtPosition = blockPosition - Vector3Int.up * i;
                            voxelMap.NewBlock(dirtPosition, 0);
                        }


                        // Ajouter des arbres aléatoirement
                        if (UnityEngine.Random.value < treeSpawnProbability && height > levelSee)
                        {
                            GenerateThree(blockPosition + Vector3Int.up);
                        }

                        int hauteurRestante = Mathf.RoundToInt(blockPosition.y - 3);

                        for (int i = 1; i <= hauteurRestante; i++)
                        {
                            if (hauteurRestante > 0)
                            {
                                Vector3Int dirtPosition = blockPosition - Vector3Int.up * i;
                                voxelMap.NewBlock(dirtPosition, 1);
                            }
                        }
                    }
                }
            }
        }


        return chunk;
    }

    public Chunk ShowChunk(Vector2Int pos)
    {
        RequestMapData(pos, OnMeshDATAReceived);
        Chunk chunk = voxelMap.chunks[pos.x, pos.y];
        return chunk;
    }

    void OnMeshDATAReceived(Chunk chunk)
    {
        chunk.ReMesh(voxelMap.materials);
    }

    void GenerateThree(Vector3Int position)
    {
        int trunkHeight = 4; // Hauteur du tronc de l'arbre
        int crossSize = 3; // Taille des feuilles (carré 5x5)
        int square3x3Size = 3; // Taille de la croix (3x3)
        int square5x5Size = 5; // Taille de la croix (3x3)

        // Génération du tronc
        for (int i = 0; i < trunkHeight; i++)
        {
            Vector3Int trunkPosition = position + Vector3Int.up * i;
            voxelMap.NewBlock(trunkPosition, 4);
        }

        // Génération du deuxième carré de 5x5
        for (int x = -square5x5Size / 2; x <= square5x5Size / 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int z = -square5x5Size / 2; z <= square5x5Size / 2; z++)
                {
                    Vector3Int square5x5Position = position + Vector3Int.up * 3 + new Vector3Int(x, y, z);
                    voxelMap.NewBlock(square5x5Position, 5);
                }
            }
        }

        // Génération du carré de 3x3
        for (int x = -square3x3Size / 2; x <= square3x3Size / 2; x++)
        {
            for (int z = -square3x3Size / 2; z <= square3x3Size / 2; z++)
            {
                Vector3Int square3x3Position = position + Vector3Int.up * 5 + new Vector3Int(x, 0, z);
                voxelMap.NewBlock(square3x3Position, 5);
            }
        }

        // Génération de la croix de 3x3
        for (int x = -crossSize / 2; x <= crossSize / 2; x++)
        {
            for (int z = -crossSize / 2; z <= crossSize / 2; z++)
            {
                Vector3Int crossPosition = position + Vector3Int.up * (6) + new Vector3Int(x, 0, z);
                if(Mathf.Abs(x) == 0 || Mathf.Abs(z) == 0)
                    voxelMap.NewBlock(crossPosition, 5);
            }
        }

    }

    void GenerateTunnelNetwork(Vector3 position)
    {
        int tunnelSize = UnityEngine.Random.Range(tunnelMinSize, tunnelMaxSize);

        Vector3 tunnelDirection = UnityEngine.Random.onUnitSphere; // Direction aléatoire en 3D

        for (int i = 0; i < tunnelSize; i++)
        {
            position += tunnelDirection;

            // Creusez un tunnel principal
            DigSphere(position, 1.5f);

            // Ajoutez du bruit au tunnel pour le rendre plus naturel
            tunnelDirection += UnityEngine.Random.onUnitSphere * tunnelNoise;

            // Divisez le tunnel en deux avec une certaine probabilité
            if (UnityEngine.Random.value < 0.2f)
            {
                GenerateBranch(position, UnityEngine.Random.onUnitSphere);
            }
        }
    }

    void GenerateBranch(Vector3 position, Vector3 branchDirection)
    {
        // Générez une branche à partir de la position donnée
        int branchSize = UnityEngine.Random.Range(tunnelMinSize / 2, tunnelMaxSize / 2);

        for (int i = 0; i < branchSize; i++)
        {
            position += branchDirection;

            // Creusez la branche
            DigSphere(position, 1.5f);

            // Ajoutez du bruit à la branche pour la rendre plus naturelle
            branchDirection += UnityEngine.Random.onUnitSphere * tunnelNoise;

            // Divisez la branche en deux avec une certaine probabilité
            if (UnityEngine.Random.value < 0.01f)
            {
                GenerateBranch(position, branchDirection);
            }
        }
    }

    void DigSphere(Vector3 center, float radius)
    {
        // Calculez le rayon au carré pour une vérification plus efficace
        float radiusSquared = radius * radius;

        // Parcourez tous les blocs dans un cube autour du centre de la sphère
        int radiusCeiling = Mathf.CeilToInt(radius);
        for (int x = -radiusCeiling; x <= radiusCeiling; x++)
        {
            for (int y = -radiusCeiling; y <= radiusCeiling; y++)
            {
                for (int z = -radiusCeiling; z <= radiusCeiling; z++)
                {
                    Vector3 blockPosition = new Vector3(x, y, z) + center;

                    // Vérifiez si le bloc est à l'intérieur de la sphère
                    if ((blockPosition - center).sqrMagnitude <= radiusSquared)
                    {
                        // Creusez le bloc
                         
                        DigTunnel(blockPosition);// Utilisez le type de bloc approprié pour l'espace vide
                    }
                }
            }
        }
    }

    void DigTunnel(Vector3 position)
    {
        // Creusez un bloc à la position donnée
        voxelMap.NewBlock(Vector3Int.RoundToInt(position), -1); // Utilisez le type de bloc approprié pour l'espace vide
    }

    float GeneratePerlinNoise2D(float x, float z, float zoom, int octaves, float persistence, float heightScale)
    {
        x *= zoom;
        z *= zoom;
        x += zoom;
        z += zoom;

        float totalNoise = 0;
        float frequency = 1.0f;
        float amplitude = 1.0f;
        float maxValue = 0; // Utilisé pour normaliser le résultat

        for (int i = 0; i < octaves; i++)
        {
            totalNoise += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;

            maxValue += amplitude;

            amplitude *= persistence; // Réduit l'amplitude à chaque octave
            frequency *= 2;
        }

        // Normalisation du résultat entre 0 et 1, puis mise à l'échelle par la hauteur
        float scaledNoise = totalNoise / maxValue * heightScale;

        return scaledNoise;
    }

    //requette pour generé le chunk sur un autre thread
    public void RequestMapData(Vector2Int chunkPosition, Action<Chunk> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(chunkPosition, voxelMap.chunks, callback);
        };

        new Thread(threadStart).Start();
    }

    //lancement du theard, le thread ne peut pas communiqué avec les autres variables il faut donc tout lui donné avant de le lancé, et on récupère par callback
    public Chunk MapDataThread(Vector2Int chunkPosition, Chunk[,] chunks, Action<Chunk> callback)
    {
        Chunk chunk = voxelMap.ShowChunk(chunkPosition, chunks);
        MeshBlock[,,] meshMap = chunk.meshMap;
        lock (mapdataInfoQueue)
        {
            mapdataInfoQueue.Enqueue(new MapThreadInfo<Chunk>(callback, chunk));
        }
        return chunk;
    }


    //element de la liste d'attente pour les chunks
    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}