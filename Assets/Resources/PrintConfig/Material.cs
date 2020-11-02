using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrintMaterial : MonoBehaviour
{
    public GameObject MaterialObject;

    private static PrintMaterial singleton = null;
    public static PrintMaterial Singleton
    {
        get
        {
            if (singleton == null)
            {
                throw new InvalidOperationException("Cannot fetch Material before it has been initialized");
            }

            return singleton;
        }
    }

    public static PrintMaterial InitializaSingleton(GameObject owner)
    {
        if (singleton != null)
        {
            throw new InvalidOperationException("We do not allow more than one Material object");
        }

        singleton = owner.AddComponent<PrintMaterial>();
        singleton.MaterialObject = Resources.Load<GameObject>("PrintConfig/MaterialObject");
        return singleton;
    }

    public GameObject InstantiateObject()
    {
        return Instantiate(MaterialObject);
    }
}
