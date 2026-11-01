using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GameObject tilePrefab;

    // Harita boyutlarını buradan belirleyebilirsin (Şimdilik 20x20 ideal)
    public int gridWidth = 1000;
    public int gridHeight = 1000;

    // Bütün kareleri hafızada tutacağımız veri matrisimiz
    [HideInInspector]
    public TileScript[,] gridMatrix;

    void Awake()
    {
        // Matrisi belirlediğimiz genişlik ve yüksekliğe göre oluşturuyoruz
        gridMatrix = new TileScript[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Kareyi sahnede üret (o gereksiz transform çağırmasından kurtulduk)
                GameObject newTile = Instantiate(tilePrefab, transform);

                // Pozisyonunu ayarla
                newTile.transform.position = new Vector3(x, y, 0);

                // Ürettiğimiz karenin TileScript bileşenini al
                TileScript tileScript = newTile.GetComponent<TileScript>();

                // Koordinatları scriptin içine kaydet
                tileScript.gridX = x;
                tileScript.gridY = y;

                // İleride aradığımızda anında bulabilmek için kareyi matrise yerleştir
                gridMatrix[x, y] = tileScript;
            }
        }
    }
}