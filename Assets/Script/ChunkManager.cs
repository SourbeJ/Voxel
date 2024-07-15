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
        // Initialisez la position du joueur au début
        lastPlayerPosition = transform.position;

        // Calculez le chunk initial
        currentPlayerChunk = GetChunkPosition(lastPlayerPosition);

        //UpdateLoadedChunks(GetChunksAroundPosition(transform.position, renderDistance));
    }

    private void Update()
    {



        // Obtenez la position actuelle du joueur
        Vector3 currentPlayerPosition = transform.position;




        // Vérifiez si la position du joueur a changé
        if (currentPlayerPosition != lastPlayerPosition)
        {
            //generation procedurale

            // Déterminez les chunks autour de la nouvelle position du joueur
            List<Vector2Int> newChunksNotLoadedAroundPlayer = GetChunksAroundPosition(currentPlayerPosition, renderDistance + 1);

            // Cherchez les chunks à charger (ceux qui ne sont pas encore chargés)
            List<Vector2Int> chunksNotLoadedToLoad = new List<Vector2Int>(newChunksNotLoadedAroundPlayer);

            // Chargez les nouveaux chunks
            foreach (var chunkPosition in chunksNotLoadedToLoad)
            {
                LoadChunk(chunkPosition);
            }

            //affichage du chunk

            // Déterminez les chunks actuellement chargés autour de l'ancienne position du joueur
            List<Vector2Int> oldChunksAroundPlayer = GetChunksAroundPosition(lastPlayerPosition, renderDistance);

            // Déterminez les chunks autour de la nouvelle position du joueur
            List<Vector2Int> newChunksAroundPlayer = GetChunksAroundPosition(currentPlayerPosition, renderDistance);

            // Cherchez les chunks à décharger (ceux qui étaient chargés précédemment mais qui ne le sont plus)
            List<Vector2Int> chunksToUnload = new List<Vector2Int>(oldChunksAroundPlayer);
            chunksToUnload.RemoveAll(chunk => newChunksAroundPlayer.Contains(chunk));

            // Cherchez les chunks à charger (ceux qui ne sont pas encore chargés)
            List<Vector2Int> chunksToLoad = new List<Vector2Int>(newChunksAroundPlayer);
            chunksToLoad.RemoveAll(chunk => loadedChunksShow.Contains(chunk));

            // Déchargez les chunks obsolètes
            foreach (var chunkPosition in chunksToUnload)
            {
                UnloadChunk(chunkPosition);
            }

            // Chargez les nouveaux chunks
            foreach (var chunkPosition in chunksToLoad)
            {
                ShowChunk(chunkPosition);
            }



            // Mettez à jour la position du joueur pour la prochaine itération
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

        // Parcourez les chunks dans un carré autour de la position spécifiée
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
        // Généré la matrice de block du chunk
        if (chunkPosition.x >= 0 && chunkPosition.y >= 0 && chunkPosition.x < 64 && chunkPosition.y < 64)
            voxelMapGenerator.GenerateChunk(chunkPosition);
            
    }

    void ShowChunk(Vector2Int chunkPosition)
    {
        // Créez un nouveau chunk et l'ajoutez à la liste des chunks chargés
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
        // Déchargez un chunk en le détruisant et en le retirant de la liste des chunks chargés
        if (loadedChunksShow.Contains(chunkPosition))
        {
            //Debug.Log("Unload " + chunkPosition);
            Destroy(VoxelMap.chunks[chunkPosition.x, chunkPosition.y].cube.gameObject);
            loadedChunksShow.Remove(chunkPosition);
            VoxelMap.chunks[chunkPosition.x, chunkPosition.y] = new Chunk();
        }
    }
}