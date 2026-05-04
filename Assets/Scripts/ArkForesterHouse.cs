using UnityEngine;

public class ArkForesterHouse : MonoBehaviour
{
    [SerializeField] private GameObject[] dogPrefabs;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.15f, -2.4f);

    private bool dogSpawned;

    public void SetDogPrefabs(GameObject[] prefabs)
    {
        dogPrefabs = prefabs;
    }

    public void SpawnDog()
    {
        if (dogSpawned || dogPrefabs == null || dogPrefabs.Length == 0)
        {
            return;
        }

        GameObject prefab = dogPrefabs[Random.Range(0, dogPrefabs.Length)];
        if (prefab == null)
        {
            return;
        }

        Vector3 spawnPosition = transform.TransformPoint(spawnOffset);
        GameObject dogObject = Instantiate(prefab, spawnPosition, transform.rotation);
        dogObject.name = "Собака лесника";
        ArkDogWorker dog = dogObject.GetComponent<ArkDogWorker>();
        if (dog == null)
        {
            dog = dogObject.AddComponent<ArkDogWorker>();
        }

        dogSpawned = true;
        if (ArkGameManager.Instance != null)
        {
            ArkGameManager.Instance.RegisterDog(dog);
            ArkGameManager.Instance.ShowMessage("Из домика лесника выбежала собака.");
        }
    }
}
