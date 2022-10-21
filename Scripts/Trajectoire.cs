using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trajectoire : MonoBehaviour
{

    // Liste des point + rotations
    private List<Vector3> Positions;
    private List<Quaternion> Rotations;
    private List<Vector3> newPositions;
    private List<Quaternion> newRotations;

    // Déclaration des types
    public enum EMethodeType { Lagrange, Bezier, Spline };

    [Header("Choix de la méthode")]
    public EMethodeType Methode = EMethodeType.Lagrange;

    [Header("Paramètres de Lagrange/Bezier")]
    // Pas d'échantillonnage 
    public float pas = 1 / 100;
    //Fermeture du polygone de contrôle
    public bool fermeture = false;
    // Vecteur des pas temporels
    private List<float> T = new List<float>();
    // Echantillonage des pas temporels
    private List<float> tToEval = new List<float>();

    [Header("Paramètres de Spline (toujours fermée)")]
    // Nombre de subdivisions pour les splines
    public int nbSubdivision = 3;
    // Degré de la spline
    public int degre = 3;

    // indice de parcours de la trajectoire calculée
    private int index = 0;

    // Start is called before the first frame update
    void Start()
    {
        // Recupérer les points de contrôle
        GameObject[] arrayPC = GameObject.FindGameObjectsWithTag("PointControle");
        int nbPoints = arrayPC.Length;

        // Ordonner les PCs
        List<GameObject> listPC = new List<GameObject>(arrayPC);
        listPC.Sort(delegate (GameObject c1, GameObject c2) {
                int ordre1 = c1.GetComponent<CameraPos>().ordrePassage;
                int ordre2 = c2.GetComponent<CameraPos>().ordrePassage;
                return ordre1.CompareTo(ordre2);
            });
        // Fermer le polygone de controle
        if (fermeture || (Methode==EMethodeType.Spline))
        {
            listPC.Add(listPC[0]);
            nbPoints++;
        }

        // Construire les listes des paramètres
        Positions = new List<Vector3>();
        Rotations = new List<Quaternion>();
        foreach (GameObject obj in listPC)
        {
            Positions.Add(obj.GetComponent<Transform>().position);
            Rotations.Add(obj.GetComponent<Transform>().rotation);
        }


        // Construire la paramétrisation
        T = buildParametrisationReguliere(nbPoints);

        // Construire des échantillons
        tToEval = echantillonage(pas);

        // Appliquer la paramétrisation
        newPositions = new List<Vector3>();
        newRotations = new List<Quaternion>();
        switch (Methode)
        {
            case EMethodeType.Lagrange:
                for (int i = 0; i < tToEval.Count; i++)
                {
                    float t = tToEval[i];
                    newPositions.Add(neville(t));
                    newRotations.Add(getRotation(t));
                }
                break;
            case EMethodeType.Bezier:
                for (int i = 0; i < tToEval.Count; i++)
                {
                    float t = tToEval[i];
                    newPositions.Add(DeCasteljau(t));
                    newRotations.Add(getRotation(t));
                }
                break;
            case EMethodeType.Spline:
                (newPositions, newRotations) = spline();
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Déplacer la caméra
        transform.position = newPositions[index];
        transform.rotation = newRotations[index];

        // Incrémenter 
        index = (index + 1) % (newPositions.Count - 1);
    }

    // Dessiner la trajectoire
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < newPositions.Count - 1; i++)
            {
                Gizmos.DrawLine(newPositions[i], newPositions[i + 1]);
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////
    // fonction : buildParametrisationReguliere                             //
    // semantique : construit la parametrisation reguliere                  //
    // params :                                                             //
    //          - int nbElem : nombre d'elements de la parametrisation      //
    // sortie :                                                             //
    //          - List<float> T : parametrisation reguliere                 //
    //////////////////////////////////////////////////////////////////////////
    List<float> buildParametrisationReguliere(int nbElem)
    {
        List<float> T = new List<float>();

        // Construction des pas temporels
        for (int i = 0; i < nbElem; i++)
        {
            T.Add(i / (float)(nbElem - 1));
        }

        return T;
    }

    //////////////////////////////////////////////////////////////////////////
    // fonction : Echantillonage                                            //
    // semantique : construit les échantillons de temps entre 0 et 1        //
    // params :                                                             //
    //          - int nbElem : nombre d'elements de la parametrisation      //
    //          - float pas : pas d'échantillonage                          //
    // sortie :                                                             //
    //          - List<float> tToEval : echantillons                        //
    //////////////////////////////////////////////////////////////////////////
    List<float> echantillonage(float pas)
    {
        List<float> tToEval = new List<float>();

        // Construction des échantillons
        int size = (int)(1.0 / pas);
        for (int i = 0; i < size; i++)
        {
            tToEval.Add(i * pas);
        }
        tToEval.Add(1.0f);

        return tToEval;
    }

    //////////////////////////////////////////////////////////////////////////
    // fonction : neville                                                   //
    // semantique : calcule le point atteint par la courbe en t sachant     //
    //              qu'elle passe par les Positions en T                    //
    // params : temps de l'interpolation                                    //
    // sortie : vecteur représentant la position en t                       //
    //////////////////////////////////////////////////////////////////////////
    private Vector3 neville(float t)
    {
        // Initialisation
        int size = Positions.Count;
        List<float> Tg = new List<float>(T);
        List<float> Td = new List<float>(T);
        List<Vector3> points = new List<Vector3>(Positions);

        int nbiter = size - 1;
        for (int k = 0; k < nbiter; k++)
        {
            List<Vector3> newPoints = new List<Vector3>();
            List<float> newTg = new List<float>();
            List<float> newTd = new List<float>();
            for (int i = 0; i < size - 1; i++)
            {
                float alpha = (Td[i + 1] - t) / (Td[i + 1] - Tg[i]);
                newTg.Add(Tg[i]);
                newTd.Add(Td[i + 1]);
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                newPoints.Add(alpha * a + (1 - alpha) * b);
            }
            size = newPoints.Count;
            points = newPoints;
            Tg = newTg;
            Td = newTd;
        }

        return points[0];
    }

    //////////////////////////////////////////////////////////////////////////
    // fonction : DeCasteljau                                               //
    // semantique : renvoie le point approxime via l'aglgorithme de DCJ     //
    //              pour une courbe définie par les points de controle      //
    //              à l'instant t                                           //
    // params :  float t : temps d'approximation                            //
    // sortie : vecteur représentant la position en t                       //
    //////////////////////////////////////////////////////////////////////////
    Vector3 DeCasteljau(float t)
    {
        // Initialisation
        int size = Positions.Count;
        List<Vector3> points = new List<Vector3>(Positions);

        int nbiter = size - 1;
        for (int k = 0; k < nbiter; k++)
        {
            List<Vector3> newPoints = new List<Vector3>();
            for (int i = 0; i < size - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                newPoints.Add((1 - t) * a + t * b);
            }
            size = newPoints.Count;
            points = newPoints;
        }
        return points[0];
    }

    //////////////////////////////////////////////////////////////////////////
    // fonction : spline                                                    //
    // semantique : interpole les points de controle en utilisant           //
    //              les splines (ferme toujours le polygone )               //
    // sortie : listes des nouvelles positions et rotations                 //
    //////////////////////////////////////////////////////////////////////////
    (List<Vector3>, List<Quaternion>) spline()
    {
        // Interpoler les Positions
        List<Vector3> startpoints = new List<Vector3>(Positions);
        List<Quaternion> startrots = new List<Quaternion>(Rotations);
        for (int k = 0; k < nbSubdivision; k++)
        {
            List<Vector3> points = new List<Vector3>();
            List<Quaternion> rots = new List<Quaternion>();
            // Dupliquer
            for (int j = 0; j < startpoints.Count - 1; j++)
            {
                points.Add(startpoints[j]);
                points.Add(startpoints[j]);
                rots.Add(startrots[j]);
                rots.Add(startrots[j]);
            }
            points.Add(startpoints[startpoints.Count - 1]);
            rots.Add(startrots[startpoints.Count - 1]);

            for (int m = 0; m < degre; m++)
            {
                List<Vector3> newPoints = new List<Vector3>();
                List<Quaternion> newRots = new List<Quaternion>();
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Vector3 np = (1.0f / 2) * points[i] + (1.0f / 2) * points[i + 1];
                    newPoints.Add(np);
                    Quaternion nr = Quaternion.Slerp(rots[i], rots[i + 1], 0.5f); ;
                    newRots.Add(nr);
                }
                points = newPoints;
                rots = newRots;
                points.Add(newPoints[0]); //Fermer la courbe
                rots.Add(newRots[0]);  //Fermer la courbe
            }

            startpoints = points;
            startrots = rots;
        }

        return (startpoints, startrots);
    }

    //////////////////////////////////////////////////////////////////////////
    // fonction : getRotation                                               //
    // semantique : réalise l’interpolation sphérique entre deux quaternions//
    //              à l'instant t                                           //
    // params :                                                             //
    //          - float t : l'instant où on cherche la rotation             //
    // sortie :                                                             //
    //          - Quaternion : la rotation à cet instant                    //
    //////////////////////////////////////////////////////////////////////////
    Quaternion getRotation(float t)
    {

        float delta = 1.0f / (Positions.Count - 1);
        int i_0 = (int)Mathf.Floor(t / delta);
        
        if (i_0 == Positions.Count - 1)
        {
            return Rotations[i_0]; // Dernier point t=1
        } 
        else
        {
            float t_local = (t - i_0 * delta) / delta;
            return Quaternion.Slerp(Rotations[i_0], Rotations[i_0 + 1], t_local);
        }

    }
}
