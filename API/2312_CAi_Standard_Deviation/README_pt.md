# Estratégia CAi de Desvio Padrão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port para StockSharp do expert MQL5 original **Exp_i-CAi_StDev**. Combina uma média móvel com bandas de desvio padrão para detectar rompimentos e reversões subsequentes.

## Lógica da estratégia

1. Calcular uma média móvel simples (SMA) ao longo do período especificado.
2. Calcular o desvio padrão dos preços de fechamento no mesmo período.
3. Construir dois conjuntos de bandas em torno da SMA:
   - **Bandas de entrada**: SMA ± `OpenMultiplier` × StdDev.
   - **Bandas de saída**: SMA ± `CloseMultiplier` × StdDev.
4. Abrir uma posição comprada quando o preço fecha acima da banda de entrada superior.
5. Abrir uma posição vendida quando o preço fecha abaixo da banda de entrada inferior.
6. Fechar uma posição comprada existente quando o preço cai abaixo da banda de saída superior.
7. Fechar uma posição vendida existente quando o preço sobe acima da banda de saída inferior.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `MaLength` | Comprimento do cálculo da média móvel e desvio padrão | 12 |
| `StdDevPeriod` | Período para o indicador de desvio padrão | 9 |
| `OpenMultiplier` | Multiplicador para bandas de entrada | 2.5 |
| `CloseMultiplier` | Multiplicador para bandas de saída | 1.5 |
| `CandleType` | Tipo de candles usado pela estratégia | Candles de 5 minutos |

## Notas

- A estratégia usa a API de alto nível com `Bind` para receber valores do indicador.
- Apenas candles concluídos são processados para evitar sinais prematuros.
- Todos os comentários no código-fonte estão em inglês.
