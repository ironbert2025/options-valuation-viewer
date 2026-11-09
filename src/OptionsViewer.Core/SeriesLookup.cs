namespace OptionsViewer.Core;

public static class SeriesLookup
{
    /// <summary>
    /// Índice del punto más cercano a <paramref name="x"/> en el arreglo ordenado de menor a mayor <paramref name="xs"/>
    /// (los <see cref="SeriesPoint.SecondsOfDay"/> de la serie), con búsqueda binaria. Cuenta cualquier snapshot, tenga o
    /// no datos el contrato (RF-07, D8). Antes del primero devuelve 0, después del último el último índice, y en un
    /// empate exacto el punto anterior. Devuelve -1 si el arreglo está vacío o <paramref name="x"/> es NaN.
    /// </summary>
    public static int FindNearestIndex(double[] xs, double x)
    {
        if (xs.Length == 0 || double.IsNaN(x))
            return -1;

        // Primer índice con xs[i] >= x
        int lo = 0, hi = xs.Length;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (xs[mid] < x)
                lo = mid + 1;
            else
                hi = mid;
        }

        if (lo == 0)
            return 0;
        if (lo == xs.Length)
            return xs.Length - 1;

        // Entre xs[lo - 1] (< x) y xs[lo] (>= x): gana el más cercano; si empatan, el anterior
        return x - xs[lo - 1] <= xs[lo] - x ? lo - 1 : lo;
    }
}
