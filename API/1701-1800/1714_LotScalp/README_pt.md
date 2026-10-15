# Estratégia LotScalp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre uma única operação por dia em uma hora especificada com base na diferença entre as aberturas de velas passadas.

## Como Funciona

1. **Aguardar o Horário de Negociação**: A estratégia monitora os horários de abertura das velas. Uma vez que a hora seja maior que `TradeTime`, a negociação é permitida na próxima ocorrência dessa hora.
2. **Geração de Sinais**:
   - Quando a hora atual é igual a `TradeTime`, a estratégia compara o preço de abertura de `t1` barras atrás com o preço de abertura de `t2` barras atrás.
   - Se a diferença `Open[t1] - Open[t2]` exceder `DeltaShort` pontos, uma posição vendida é aberta.
   - Se a diferença `Open[t2] - Open[t1]` exceder `DeltaLong` pontos, uma posição comprada é aberta.
3. **Gestão de Posições**:
   - Para posições compradas, a estratégia sai quando o preço alcança `TakeProfitLong` acima da entrada ou `StopLossLong` abaixo dela.
   - Para posições vendidas, sai quando o preço se move `TakeProfitShort` abaixo ou `StopLossShort` acima da entrada.
   - As posições também são fechadas se permanecerem abertas por mais de `MaxOpenTime` horas.

A estratégia opera com volume fixo e não entra em novas operações até o dia seguinte.

## Parâmetros

| Nome | Descrição |
| ---- | --------- |
| `CandleType` | Fonte de velas para a estratégia. |
| `Volume` | Volume da ordem. |
| `TakeProfitLong` | Take profit em pontos para operações compradas. |
| `StopLossLong` | Stop loss em pontos para operações compradas. |
| `TakeProfitShort` | Take profit em pontos para operações vendidas. |
| `StopLossShort` | Stop loss em pontos para operações vendidas. |
| `TradeTime` | Hora do dia em que os sinais são avaliados. |
| `T1` | Número de barras atrás para o primeiro preço de abertura. |
| `T2` | Número de barras atrás para o segundo preço de abertura. |
| `DeltaLong` | Diferença mínima (em pontos) entre `Open[t2]` e `Open[t1]` para abrir uma operação comprada. |
| `DeltaShort` | Diferença mínima (em pontos) entre `Open[t1]` e `Open[t2]` para abrir uma operação vendida. |
| `MaxOpenTime` | Tempo máximo de manutenção em horas. |

## Notas

- Apenas velas concluídas são processadas.
- A estratégia usa o passo de preço do instrumento para converter limites baseados em pontos em preços absolutos.
- Nenhum indicador adicional é utilizado.
