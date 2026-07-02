# Estratégia de IMF do CDC PL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **CDC PL MFI Strategy** reproduz o MetaTrader consultor especialista `Expert_ADC_PL_MFI` (MQL/299) em StockSharp. Ele procura os padrões de reversão de duas velas **Dark Cloud Cover** e **Piercing Line** e valida cada sinal com o oscilador **Money Flow Index (MFI)**. The strategy uses the same indicator periods and level thresholds as the original expert, adds optional stop-loss and take-profit protection in pip units, and closes positions when the MFI crosses configurable reversal levels.

## Lógica de negociação
1. Assine o tipo de vela configurado (velas de uma hora por padrão) e calcule um Índice de Fluxo de Dinheiro com o período especificado. Mantenha médias móveis simples do tamanho do corpo da vela e dos preços de fechamento para replicar a tendência original e os filtros de volatilidade.
2. Quando um padrão de alta **Piercing Line** se forma (gap abaixo da mínima anterior, fechamento de alta acima do ponto médio da vela de baixa anterior, ambas as velas maiores que o corpo médio e o fechamento anterior abaixo da média da tendência) *e* o valor MFI atual está abaixo de **LongEntryLevel** (padrão `40`), entre ou mude para uma posição longa.
3. Quando um padrão de baixa **Dark Cloud Cover** se forma (gap acima da máxima anterior, fechamento de baixa abaixo do ponto médio da vela de alta anterior, ambas as velas maiores que o corpo médio e o fechamento anterior acima da média da tendência) *e* o valor MFI atual está acima de **ShortEntryLevel** (padrão `60`), entre ou mude para uma posição curta.
4. Monitorizar a IMF para fechar posições de forma proactiva:
   - Fechar posições curtas quando a IMF ultrapassar **ExitLowerLevel** (`30`) ou **ExitUpperLevel** (`70`).
   - Feche posições longas quando a IMF cruzar abaixo de **ExitUpperLevel** (`70`) ou **ExitLowerLevel** (`30`).
5. Ordens de proteção são opcionais. Quando **TakeProfitPips** ou **StopLossPips** são maiores que zero, a estratégia chama `StartProtection` com as compensações de preço correspondentes (distância do pip multiplicada pela etapa do preço do título).

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Tipo de dados Candle usado para detecção de padrões. | `1 hour` período de tempo |
| `MfiPeriod` | Comprimento do oscilador do Índice de Fluxo de Dinheiro. | `49` |
| `BodyAveragePeriod` | Período da média móvel do corpo da vela usado para qualificar velas “longas”. | `11` |
| `LongEntryLevel` | Limite de MFI que confirma configurações de alta da Piercing Line. | `40` |
| `ShortEntryLevel` | MFI threshold that confirms bearish Dark Cloud Cover setups. | `60` |
| `ExitLowerLevel` | Nível mais baixo da IMF que aciona a cobertura de posições curtas. | `30` |
| `ExitUpperLevel` | Nível superior da IMF que aciona o fechamento de posições longas. | `70` |
| `StopLossPips` | Distância de stop-loss opcional em pips (0 desativa a proteção). | `50` |
| `TakeProfitPips` | Distância opcional de take-profit em pips (0 desativa a proteção). | `50` |

## Notas
- O volume padrão é `1` lote. Quando a estratégia muda de direção, ela envia uma única ordem de mercado dimensionada para fechar a posição existente e abrir a nova, correspondendo ao comportamento MQL.
- A detecção de padrões reflete a lógica MetaTrader: apenas velas concluídas são avaliadas, as lacunas devem ocorrer além da máxima/mínima anterior e uma média móvel simples impõe a condição de tendência predominante.
- Os valores do Índice de Fluxo de Dinheiro vêm diretamente do indicador vinculado. Nenhum buffer manual do histórico do indicador é necessário; a estratégia armazena apenas os valores mais recentes para detectar cruzamentos de limites.
- Nenhuma porta Python é fornecida; apenas a implementação do C# está incluída neste diretório.
