using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BlockPool : MonoBehaviour
{
    private ObjectPool<Block> _pool;
    [SerializeField] private Block _prefab;
    [SerializeField] private Transform _parent;
    [SerializeField] private int defaultCapacity = 50;
    [SerializeField] private int maxSize = 100;
    
    void Start()
    {
        _pool = new ObjectPool<Block>(
            CreatePooledItem,
            OnTakeFromPool,
            OnReturnedToPool,
            OnDestroyPooledObject,
            collectionCheck: false,
            defaultCapacity,
            maxSize);
            
        PreWarmPool();
    }

    private void PreWarmPool()
    {
        List<Block> tempBlocks = new List<Block>();
        for (int i = 0; i < defaultCapacity; i++)
        {
            tempBlocks.Add(_pool.Get());
        }
        foreach (var block in tempBlocks)
        {
            _pool.Release(block);
        }
    }

    private Block CreatePooledItem()
    {
        Block instance = Object.Instantiate(_prefab, _parent);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void OnTakeFromPool(Block instance)
    {
        instance.gameObject.SetActive(true);
    }

    private void OnReturnedToPool(Block instance)
    {
        instance.gameObject.SetActive(false);
    }

    private void OnDestroyPooledObject(Block instance)
    {
        Object.Destroy(instance.gameObject);
    }

    public Block Get() => _pool.Get();
    public void Release(Block instance) => _pool.Release(instance);
}