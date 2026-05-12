using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class TissueComponent : Entity, ISupportedMultiEntity
{
    public GameObject go;
    public GameObject UnUse;
    public GameObject Used;
    public bool isUsed = false;

    public void Init(Transform tissueParent)
    {
        Load(tissueParent).Coroutine();
    }

    public async FTask Load(Transform tissueParent)
    {
        var bundle = await Scene.GetComponent<ResourceLoaderComponent>().LoadAssetAsync<GameObject>("L1_Tissue");
        go = GameObject.Instantiate(bundle, tissueParent);
        var rc = go.GetComponent<ReferenceCollector>();
        UnUse = rc.Get<GameObject>("L1_TissueReused");
        Used = rc.Get<GameObject>("L1_TissueRubbish");

        go.name = Id.ToString();
        UnUse.SetActive(false);
        Used.SetActive(false);

        var unityEventTrigger = go.GetComponent<UnityEventTrigger>();
        unityEventTrigger.Register(OnCollisionEnter2D);
        go.SetActive(false);
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        var layer = LayerMask.LayerToName(collision.gameObject.layer);
        if (layer == "Rubbish" && isUsed)
            Dispose();
    }

    public void OnCollisionExit2D(Collision2D collision)
    {
    }

    public void ChangeState(bool isUsed)
    {
        this.isUsed = isUsed;
    }

    public void DropByHand(Transform tissueParent)
    {
        go.transform.SetParent(tissueParent);
        UnUse.SetActive(!isUsed);
        Used.SetActive(isUsed);
        go.SetActive(true);
    }

    public void PickUpByHand(Transform tissueParent)
    {
        go.transform.SetParent(tissueParent);
        go.SetActive(false);
    }
}

public class TissueComponent_Destroy : DestroySystem<TissueComponent>
{
    protected override void Destroy(TissueComponent self)
    {
        if (self.go != null)
            GameObject.Destroy(self.go);
    }
}