using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public VoxelMapGenerator voxelMapGenerator;
    public VoxelMap VoxelMap;

    public int chunkSize = 16; // Taille d'un chunk
    public int renderDistance = 3; // Distance de rendu des chunks autour du joueur

    private Vector3 lastPlayerPosition;
    private Vector2Int currentPlayerChunk;

    List<Vector2Int> loadedChunksShow = new List<Vector2Int>();

    private void Start()
    {
        // Initialisez la position du joueur au d�but
        lastPlayerPosition = transform.position;

        // Calculez le chunk initial
        currentPlayerChunk = GetChunkPosition(lastPlayerPosition);

        //UpdateLoadedChunks(GetChunksAroundPosition(transform.position, renderDistance));
    }

    private void Update()
    {



        // Obtenez la position actuelle du joueur
        Vector3 currentPlayerPosition = transform.position;




        // V�rifiez si la position du joueur a chang�
        if (currentPlayerPosition != lastPlayerPosition)
        {
            //generation procedurale

            // D�terminez les chunks autour de la nouvelle position du joueur
            List<Vector2Int> newChunksNotLoadedAroundPlayer = GetChunksAroundPosition(currentPlayerPosition, renderDistance + 1);

            // Cherchez les chunks � charger (ceux qui ne sont pas encore charg�s)
            List<Vector2Int> chunksNotLoadedToLoad = new List<Vector2Int>(newChunksNotLoadedAroundPlayer);

            // Chargez les nouveaux chunks
            foreach (var chunkPosition in chunksNotLoadedToLoad)
            {
                LoadChunk(chunkPosition);
            }

            //affichage du chunk

            // D�terminez les chunks actuellement charg�s autour de l'ancienne position du joueur
            List<Vector2Int> oldChunksAroundPlayer = GetChunksAroundPosition(lastPlayerPosition, renderDistance);

            // D�terminez les chunks autour de la nouvelle position du joueur
            List<Vector2Int> newChunksAroundPlayer = GetChunksAroundPosition(currentPlayerPosition, renderDistance);

            // Cherchez les chunks � d�charger (ceux qui �taient charg�s pr�c�demment mais qui ne le sont plus)
            List<Vector2Int> chunksToUnload = new List<Vector2Int>(oldChunksAroundPlayer);
            chunksToUnload.RemoveAll(chunk => newChunksAroundPlayer.Contains(chunk));

            // Cherchez les chunks � charger (ceux qui ne sont pas encore charg�s)
            List<Vector2Int> chunksToLoad = new List<Vector2Int>(newChunksAroundPlayer);
            chunksToLoad.RemoveAll(chunk => loadedChunksShow.Contains(chunk));

            // D�chargez les chunks obsol�tes
            foreach (var chunkPosition in chunksToUnload)
            {
                UnloadChunk(chunkPosition);
            }

            // Chargez les nouveaux chunks
            foreach (var chunkPosition in chunksToLoad)
            {
                ShowChunk(chunkPosition);
            }



            // Mettez � jour la position du joueur pour la prochaine it�ration
            lastPlayerPosition = currentPlayerPosition;
        }
    }

    Vector2Int GetChunkPosition(Vector3 worldPosition)
    {
        int chunkX = Mathf.FloorToInt(worldPosition.x / chunkSize);
        int chunkZ = Mathf.FloorToInt(worldPosition.z / chunkSize);

        return new Vector2Int(chunkX, chunkZ);
    }

    List<Vector2Int> GetChunksAroundPosition(Vector3 position, int radius)
    {
        Vector2Int chunkPosition = GetChunkPosition(position);
        List<Vector2Int> chunksAroundPosition = new List<Vector2Int>();

        // Parcourez les chunks dans un carr� autour de la position sp�cifi�e
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                Vector2Int nearbyChunk = new Vector2Int(chunkPosition.x + x, chunkPosition.y + z);
                chunksAroundPosition.Add(nearbyChunk);
            }
        }

        return chunksAroundPosition;
    }

    void LoadChunk(Vector2Int chunkPosition)
    {
        // G�n�r� la matrice de block du chunk
        if (chunkPosition.x >= 0 && chunkPosition.y >= 0 && chunkPosition.x < 64 && chunkPosition.y < 64)
            voxelMapGenerator.GenerateChunk(chunkPosition);
            
    }

    void ShowChunk(Vector2Int chunkPosition)
    {
        // Cr�ez un nouveau chunk et l'ajoutez � la liste des chunks charg�s
        if (chunkPosition.x >= 0 && chunkPosition.y >= 0 && chunkPosition.x < 64 && chunkPosition.y < 64)
        {
            VoxelMap.chunks[chunkPosition.x, chunkPosition.y].cube = new GameObject("Chunk (" + chunkPosition.x + ";" + chunkPosition.y + ")");
            VoxelMap.chunks[chunkPosition.x, chunkPosition.y].cube.transform.position = new Vector3(chunkPosition.x * 16f, 0, chunkPosition.y * 16f);
            voxelMapGenerator.ShowChunk(chunkPosition);
            loadedChunksShow.Add(chunkPosition);
        }

    }

    void UnloadChunk(Vector2Int chunkPosition)
    {
        // D�chargez un chunk en le d�truisant et en le retirant de la liste des chunks charg�s
        if (loadedChunksShow.Contains(chunkPosition))
        {
            //Debug.Log("Unload " + chunkPosition);
            Destroy(VoxelMap.chunks[chunkPosition.x, chunkPosition.y].cube.gameObject);
            loadedChunksShow.Remove(chunkPosition);
            VoxelMap.chunks[chunkPosition.x, chunkPosition.y] = new Chunk();
        }
    }
}