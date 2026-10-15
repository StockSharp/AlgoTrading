# Estratégia Exp XFisher org v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia reproduz o especialista MetaTrader 5 **Exp_XFisher_org_v1**. Opera reversões detectadas na transformada de Fisher do preço que é adicionalmente suavizado com uma média móvel configurável. O porte do StockSharp mantém a natureza contra-tendência do robô original: quando a curva de Fisher vira para baixo após uma alta, uma posição comprada é aberta, e quando a curva vira para cima após uma queda, uma posição vendida é aberta. As posições existentes são fechadas assim que o indicador reverte na direção oposta.

O indicador auxiliar `XFisherOrgIndicator` implementado em `CS/ExpXFisherOrgV1Strategy.cs` segue a lógica do MT5:

1. Pegar o maior máximo e o menor mínimo ao longo de `Length` velas completadas.
2. Converter a fonte de preço selecionada (ver *Applied Price* abaixo) para o intervalo 0–1 usando esses extremos.
3. Aplicar o filtro recursivo `value = (wpr - 0.5) + 0.67 * value[prev]` seguido pela transformada de Fisher
   `fish = 0.5 * ln((1 + value) / (1 - value)) + 0.5 * fish[prev]`.
4. Suavizar o resultado com uma das médias móveis suportadas. O valor de Fisher suavizado forma a linha principal; a linha de sinal é simplesmente o valor da barra anterior, exatamente como na versão MQL onde o buffer #1 armazena um deslocamento de uma barra.

A conversão mantém os padrões originais (`Length = 7`, suavização Jurik de comprimento 5, fase 15, velas H4) e expõe os mesmos interruptores de habilitar/desabilitar para abrir e fechar operações compradas/vendidas.

## Regras de trading
- **Entrada comprada** – quando o valor de Fisher de `SignalBar + 1` barras atrás estava subindo (`Fisher[SignalBar+1] > Fisher[SignalBar+2]`)
  mas o valor em `SignalBar` cruza abaixo ou toca sua cópia retardada (`Fisher[SignalBar] <= Fisher[SignalBar+1]`).
- **Entrada vendida** – quando o valor de Fisher de `SignalBar + 1` barras atrás estava caindo, mas o valor em `SignalBar` cruza acima
  de sua cópia retardada.
- **Saída de posição** – a reversão oposta fecha uma posição existente antes de considerar uma nova operação. Uma saída comprada é acionada
  pela mesma condição que abre um vendido, e vice-versa.
- **Volume** – controlado por `OrderVolume`. Quando uma inversão de vendido para comprado (ou comprado para vendido) é necessária, a estratégia envia
  uma única ordem a mercado com volume suficiente para fechar a posição antiga e abrir a nova na mesma transação.

Todos os cálculos usam **apenas velas completadas**. Se `SignalBar` for zero, a vela fechada atual é usada para avaliação de sinal;
valores positivos deslocam o sinal no tempo exatamente como o input `SignalBar` do MT5.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `OrderVolume` | Volume de cada ordem a mercado. | `1` |
| `BuyOpenAllowed` / `SellOpenAllowed` | Permitir abertura de operações compradas/vendidas. | `true` |
| `BuyCloseAllowed` / `SellCloseAllowed` | Permitir fechamento de operações compradas/vendidas existentes. | `true` |
| `SignalBar` | Deslocamento (em velas fechadas) usado para ler os buffers de Fisher. | `1` |
| `Length` | Lookback para extremos de preço mais altos/mais baixos. | `7` |
| `SmoothingLength` | Período da média de suavização. | `5` |
| `Phase` | Fase Jurik (ignorada por outros métodos). | `15` |
| `SmoothingMethod` | Média móvel aplicada à saída de Fisher. | `Jjma` |
| `PriceType` | Applied price encaminhada ao indicador (fechamento, abertura, mediana, etc.). | `Close` |
| `CandleType` | Série de velas usada para o cálculo (padrão: velas de 4 horas). | `H4` |

## Mapeamento do método de suavização
O indicador original expõe um grande conjunto de kernels de suavização. O porte do StockSharp os mapeia para implementações incorporadas confiáveis:

- `Jjma`, `Jurx`, `T3` → `JurikMovingAverage` (parâmetro de fase aplicado quando a propriedade está disponível).
- `Sma`, `Ema`, `Smma`, `Lwma` → respectivas médias móveis do StockSharp.
- `Parabolic` → aproximado por `ExponentialMovingAverage` (comportamento mais próximo no StockSharp).
- `Vidya`, `Ama` → `KaufmanAdaptiveMovingAverage` (o comportamento adaptativo VIDYA é modelado com Kaufman AMA).

Este mapeamento espelha a abordagem usada em outras conversões de Kositsin no repositório e mantém a resposta da linha de Fisher suavizada comparável à implementação do MT5.

## Diferenças do especialista MT5
- **Gerenciamento de capital** – as estratégias do StockSharp operam em volumes explícitos. Os inputs `MM`/`MarginMode` do MT5 são substituídos por um único parâmetro `OrderVolume` para que o trader possa definir o tamanho do lote diretamente.
- **Modelo de execução** – as operações são geradas uma vez por vela completada via API de subscrição de alto nível em vez de a cada tick. Isso evita ordens duplicadas e elimina a necessidade do helper `IsNewBar` original.
- **Opções de applied price** – todos os modos de preço de `SmoothAlgorithms.mqh` são suportados, incluindo variantes TrendFollow e Demark.
- **Charting** – a estratégia desenha velas, a transformada de Fisher suavizada e as operações executadas na área de gráfico padrão.

## Arquivos
- `CS/ExpXFisherOrgV1Strategy.cs` – classe de estratégia, implementação do indicador e container de valores.
