# Estratégia Exp i-KlPrice Vol Direto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia Exp i-KlPrice Vol Direto** é uma adaptação StockSharp do consultor especialista MetaTrader 5
`Exp_i-KlPrice_Vol_Direct`. O sistema original multiplica um oscilador KlPrice personalizado pelo volume, suaviza-o com várias
etapas de média móvel e reage a mudanças na inclinação da linha resultante. O port mantém a cadeia de processamento de
múltiplos estágios, expõe os mesmos parâmetros configuráveis e executa operações através da API de alto nível do StockSharp
em candles completadas.

Ideias-chave preservadas da versão MQL5:
- **Suavização de dois estágios de preço e intervalo** – os dados de preço são filtrados por uma média móvel configurável, o
  intervalo máximo-mínimo é suavizado separadamente.
- **Ponderação de volume** – a saída do oscilador é multiplicada pelo fluxo de volume selecionado antes de um filtro Jurik
  final.
- **Mapa de cor direcional** – a estratégia monitora o sinal da inclinação do oscilador suavizado.
- **Atraso de sinal** – `SignalBar` permite ao usuário exigir candles fechadas adicionais antes de agir.

## Pipeline de Processamento
1. **Seleção de Preço Aplicado** – escolher entre as mesmas doze fórmulas de preço aplicado do indicador MQL.
2. **Suavização Primária** – aplicar `PriceMethod` sobre `PriceLength` barras com `PricePhase` opcional.
3. **Suavização de Intervalo** – repetir o mesmo procedimento para o intervalo da candle (`High - Low`) usando `RangeMethod`,
   `RangeLength` e `RangePhase`.
4. **Construção do Oscilador** – calcular `(Price - (PriceMA - RangeMA)) / (2 * RangeMA) * 100 - 50`, idêntico à fórmula MQL,
   e multiplicar pelo fluxo de volume selecionado (`VolumeSource`).
5. **Filtro Jurik Final** – o oscilador ponderado por volume e o fluxo de volume bruto são ambos passados por médias móveis
   Jurik com período `ResultLength`.
6. **Detecção de Cor** – comparar o valor mais recente do oscilador suavizado com o anterior. Valores crescentes colorem a
   barra de altista (`0`), decrescentes de baixista (`1`), iguais herdam a cor anterior.

## Lógica de Trading
### Lado Comprado
- **Entrada**: quando a cor na barra de sinal (`SignalBar`) é altista (`0`) e a cor imediatamente anterior é baixista (`1`),
  abrir posição comprada se `AllowLongEntries = true` e a posição líquida atual não é positiva.
- **Saída**: se a cor da barra de sinal é altista e `AllowShortExits = true`, fechar quaisquer posições vendidas abertas.

### Lado Vendido
- **Entrada**: quando a cor da barra de sinal se torna baixista (`1`) após ser altista (`0`), abrir posição vendida se
  `AllowShortEntries = true` e a posição líquida atual não é negativa.
- **Saída**: se a cor da barra de sinal é baixista e `AllowLongExits = true`, fechar a exposição comprada existente.

## Referência de Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `CandleType` | Período das candles analisadas. | `H4` |
| `VolumeSource` | Fluxo de volume para ponderação (`Tick` ou `Real`). | `Tick` |
| `PriceMethod` / `PriceLength` / `PricePhase` | Algoritmo de suavização primário, período e fase Jurik para o preço aplicado. | `Sma`, `100`, `15` |
| `RangeMethod` / `RangeLength` / `RangePhase` | Algoritmo de suavização, período e fase para o intervalo da candle. | `Jjma`, `20`, `100` |
| `ResultLength` | Período Jurik para o oscilador ponderado por volume e o fluxo de volume. | `20` |
| `PriceMode` | Fórmula de preço aplicado (Close, Open, Median, Demark, TrendFollow0/1, etc.). | `Close` |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Multiplicadores de nível para diagnóstico visual; não alteram sinais. | `0`, `0`, `0`, `0` |
| `SignalBar` | Número de candles completamente fechadas a pular antes de avaliar a mudança de cor. | `1` |
| `AllowLongEntries` / `AllowShortEntries` | Indicadores de permissão para abrir operações compradas/vendidas. | `true` |
| `AllowLongExits` / `AllowShortExits` | Indicadores de permissão para fechar posições existentes em cor oposta. | `true` |
| `StopLossPoints` / `TakeProfitPoints` | Offsets de proteção em pontos de preço passados ao `StartProtection`. | `1000`, `2000` |

## Gestão de Risco
- Níveis de stop-loss e take-profit são traduzidos em offsets `UnitTypes.Point` e gerenciados pelo `StartProtection`. Definir
  qualquer valor como `0` para desabilitar a proteção respectiva.
- O tamanho de posição é completamente controlado por `Strategy.Volume`.
- Cores são avaliadas apenas quando a estratégia está formada, online e o trading é permitido.

## Limitações e Diferenças vs. MQL5
- Aproximações de suavização mais exóticas podem se desviar ligeiramente da saída do MT5.
- Candles do StockSharp expõem apenas o volume total.
- Modos de gestão de dinheiro do EA original não estão portados.
- Ordens são enviadas imediatamente após o fechamento da candle de sinal.
