using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoireOld : MonoBehaviour
{
    public enum EInterpolationType { Lagrange, Bezier, Spline };

    public float ralentissement = 10;
    public EInterpolationType InterpolationType;

    public int degre;

    private List<Vector3> Positions;
    private List<Quaternion> Rotations;

    private float t = 0;
    private List<float> paramReg;

    public bool boucle = false;

    // Start is called before the first frame update
    void Start()
    {
        // Recupérer les points de contrôle
        GameObject[] arrayPC = GameObject.FindGameObjectsWithTag("PointControle");
        int nbPC = arrayPC.Length;

        // Ordonner les PCs
        List<GameObject> listPC = new List<GameObject>(arrayPC);
        listPC.Sort(delegate (GameObject c1, GameObject c2)
        {
            int ordre1 = c1.GetComponent<CameraPos>().ordrePassage;
            int ordre2 = c2.GetComponent<CameraPos>().ordrePassage;
            return ordre1.CompareTo(ordre2);
        });
        if (boucle)
        {
            listPC.Add(listPC[0]);
        }

        // Affichage des ordres de passsage pour vérifier sort
        //foreach(GameObject obj in listPC)
        //{
        //    Debug.Log(obj.GetComponent<CameraPos>().ordrePassage);
        //}

        // Construire les listes des paramètres
        Positions = new List<Vector3>();
        Rotations = new List<Quaternion>();
        foreach (GameObject obj in listPC)
        {
            Positions.Add(obj.GetComponent<Transform>().position);
            Rotations.Add(obj.GetComponent<Transform>().rotation);
        }

        // Paramétrisation régulière
        paramReg = new List<float>();
        int nbElem = listPC.Count;
        for (int i = 0; i < nbElem; i++)
        {
            paramReg.Add(i / (float)(nbElem - 1));
        }


    }

    // Update is called once per frame
    void Update()
    {
        transform.position = getPosition(t);
        transform.rotation = getRotation(t);

        t = (t + Time.deltaTime / ralentissement) % 1; // Boucler entre 0 et 1

    }

    /////////////////////////////////////////////////////////////////////////////////////
    /// ADD COMMENTS / SIGNATURES
    /////////////////////////////////////////////////////////////////////////////////////

    Vector3 getPosition(float t)
    {
        if (InterpolationType == EInterpolationType.Bezier)
        {
            return bezier(t);
        }
        else if (InterpolationType == EInterpolationType.Lagrange)
        {
            return lagrange(t);
        }
        else if (InterpolationType == EInterpolationType.Spline)
        {
            return spline(t, degre);
        }
        else
        {
            return new Vector3(0, 0, 0);
        }
    }

    Quaternion getRotation(float t)
    {

        float delta = 1.0f / (Positions.Count - 1);
        int i_0 = (int)Mathf.Floor(t / delta);

        float t_local = (t - i_0 * delta) / delta;

        return Quaternion.Slerp(Rotations[i_0], Rotations[i_0 + 1], t_local);

    }


    //////////////////////////////////////////////////////////////////////////
    // fonction : bezier                                                    //
    // semantique : calcule la position en t selon Bézier                   //
    // params :                                                             //
    //          - float t : l'instant où on cherche la position             //
    // sortie : vecteur représentant la position en t                       //
    //////////////////////////////////////////////////////////////////////////
    Vector3 bezier(float t)
    {
        int n = Positions.Count - 1;

        Vector3 S_t = new Vector3(0, 0, 0);
        for (int i = 0; i <= n; i++)
        {
            S_t += Positions[i] * Bernstein(i, n, t);

        }

        return S_t;
    }

    //////////////////////////////////////////////////////////////////////////
    // fonction : Bernstein                                                 //
    // semantique : calcule la valeur en t du k-ième polynome de Bernstein  //
    //              pour un degré                                           //
    // params :                                                             //
    //          - int k : le polynome                                       //
    //          - int degre : le degre                                      //
    //          - float t : l'instant où on cherche la position             //
    // sortie : valeur en t du k-ième polynome de Bernstein pour le degré   //
    //////////////////////////////////////////////////////////////////////////
    float Bernstein(int k, int degre, float t)
    {
        float binom = KparmiN(k, degre);
        float valeur = (float)(binom * Mathf.Pow(t, k) * Mathf.Pow(1.0f - t, degre - k));
        return valeur;
    }

    ////////////////////////////////////////////////////////////////////////////
    // Fonction KparmiN                                                       //
    // Semantique : etant donnés k et n, calcule k parmi n                    //
    // Entrees : - int k                                                      //
    //           - int n                                                      //
    // Sortie : k parmi n                                                     //
    ////////////////////////////////////////////////////////////////////////////
    long KparmiN(int k, int n)
    {
        decimal result = 1;
        for (int i = 1; i <= k; i++)
        {
            result *= n - (k - i);
            result /= i;
        }
        return (long)result;
    }


    //////////////////////////////////////////////////////////////////////////
    // fonction : lagrange                                                  //
    // semantique : calcule la position en t selon Lagrange passant         //
    // params :                                                             //
    //          - float t : l'instant où on cherche la position             //
    // sortie : vecteur représentant la position en t                       //
    //////////////////////////////////////////////////////////////////////////
    Vector3 lagrange(float t)
    {
        int size = Positions.Count;

        Vector3 L_t = new Vector3(0, 0, 0);
        for (int i = 0; i < size; i++)
        {
            float l = 1;
            for (int m = 0; m < size; m++)
            {
                if (m != i)
                {
                    l = l * ((t - paramReg[m]) / (paramReg[i] - paramReg[m]));
                }
            }

            L_t += Positions[i] * l;
        }

        return L_t;
    }

    float getParamReg(int k)
    {
        if (k < paramReg.Count)
        {
            return paramReg[k];
        }
        else
        {
            return (float)k / (Positions.Count - 1);
        }
    }
    float Bspline(int k, int degre, float t)
    {
        if (degre == 0)
        {
            if ((t >= getParamReg(k)) && (t <= getParamReg(k + 1)))
            {
                return 1f;
            }
            else
            {
                return 0f;
            }
        }
        else
        {
            float alpha = (t - getParamReg(k)) / (getParamReg(k + degre) - getParamReg(k));
            float beta = (getParamReg(k + degre + 1) - t) / (getParamReg(k + degre + 1) - getParamReg(k + 1));
            return alpha * Bspline(k, degre - 1, t) + beta * Bspline(k + 1, degre - 1, t);
        }

    }

    Vector3 spline(float t, int degre)
    {
        int n = Positions.Count - 1;

        Vector3 S_t = new Vector3(0, 0, 0);
        for (int i = 0; i <= n; i++)
        {
            S_t += Positions[i] * Bspline(i, degre, t);
        }

        return S_t;
    }
}