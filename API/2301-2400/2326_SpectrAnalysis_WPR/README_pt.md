# Estratégia SpectrAnalysis WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia foi convertida a partir do expert MQL5 *Exp_i-SpectrAnalysis_WPR*.
Ela analisa a direção do indicador Williams %R e abre ou fecha posições de acordo com as viradas do indicador.

## Lógica

1. Subscrever às velas do período selecionado.
2. Calcular Williams %R com o período configurado.
3. Manter os últimos dois valores do indicador para detectar a direção ascendente ou descendente.
4. Quando o indicador vira para cima e entradas compradas são permitidas:
   - Fechar posições vendidas se habilitado.
   - Abrir uma nova posição comprada.
5. Quando o indicador vira para baixo e entradas vendidas são permitidas:
   - Fechar posições compradas se habilitado.
   - Abrir uma nova posição vendida.

Apenas velas concluídas são processadas. A estratégia não utiliza consultas históricas complexas e depende de ligações de API de alto nível.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Candle Type` | Período das velas usadas para cálculos | `4h` |
| `WPR Period` | Período do indicador Williams %R | `13` |
| `Allow Long Entry` | Permitir abrir posições compradas | `true` |
| `Allow Short Entry` | Permitir abrir posições vendidas | `true` |
| `Allow Long Exit` | Permitir fechar posições compradas | `true` |
| `Allow Short Exit` | Permitir fechar posições vendidas | `true` |

## Notas

A versão MQL original aplicava análise espectral à saída do Williams %R.
Esta conversão em C# usa o indicador Williams %R padrão e replica a lógica de sinais rastreando os valores recentes do indicador.
