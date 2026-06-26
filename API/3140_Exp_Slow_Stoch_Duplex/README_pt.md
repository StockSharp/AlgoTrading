# Estratégia de Exp Slow Stoch Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port de alto nível do StockSharp do assessor especialista MetaTrader 5 **Exp_Slow-Stoch_Duplex**. Combina dois osciladores estocásticos lentos que funcionam em períodos independentes para gerar sinais longos e curtos coordenados. Cada oscilador entrega seus próprios sinais de cruzamento, permitindo que a estratégia abra ou feche posições direcionais enquanto as ordens protetoras emulam o gerenciamento original de stop-loss e take-profit.

## Regras de negociação

- **Módulo longo**
  - Avalia o estocástico longo no período `LongCandleType`.
  - Aplica o método de suavização configurado aos valores %K e %D e os desloca `LongSignalBar` barras.
  - Abre uma posição longa quando %K cruza acima de %D (`previousK <= previousD` e `currentK > currentD`).
  - Fecha uma posição longa existente quando %K volta abaixo de %D (`currentK < currentD`).
- **Módulo curto**
  - Avalia o estocástico curto no período `ShortCandleType`.
  - Abre uma posição curta quando %K cruza abaixo de %D (`previousK >= previousD` e `currentK < currentD`).
  - Fecha uma posição curta existente quando %K volta acima de %D (`currentK > currentD`).
- As ordens são executadas com ordens de mercado. O volume enviado é igual a `TradeVolume` mais o valor absoluto da posição atual para que as reversões achatem primeiro a exposição anterior.
- Um take-profit e stop-loss protetores em pontos de preço são anexados via `StartProtection` para imitar os parâmetros de ordem do MT5.

## Parâmetros

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `LongCandleType` | `DataType` | Velas de 8 horas | Período para o oscilador estocástico longo. |
| `LongKPeriod` | `int` | 5 | Período de cálculo %K para o estocástico longo. |
| `LongDPeriod` | `int` | 3 | Período de suavização %D para o estocástico longo. |
| `LongSlowing` | `int` | 3 | Desaceleração adicional aplicada dentro do cálculo estocástico. |
| `LongSignalBar` | `int` | 1 | Número de barras fechadas usadas para avaliar o cruzamento. |
| `LongSmoothingMethod` | `SmoothingMethod` | `Smoothed` | Suavização secundária aplicada a %K e %D (None, Simple, Exponential, Smoothed, Weighted). |
| `LongSmoothingLength` | `int` | 5 | Comprimento do filtro de suavização secundária para o oscilador longo. |
| `LongEnableOpen` | `bool` | `true` | Permitir à estratégia abrir posições longas. |
| `LongEnableClose` | `bool` | `true` | Permitir à estratégia fechar posições longas. |
| `ShortCandleType` | `DataType` | Velas de 8 horas | Período para o oscilador estocástico curto. |
| `ShortKPeriod` | `int` | 5 | Período de cálculo %K para o estocástico curto. |
| `ShortDPeriod` | `int` | 3 | Período de suavização %D para o estocástico curto. |
| `ShortSlowing` | `int` | 3 | Desaceleração adicional aplicada dentro do cálculo estocástico. |
| `ShortSignalBar` | `int` | 1 | Número de barras fechadas usadas para avaliar o cruzamento curto. |
| `ShortSmoothingMethod` | `SmoothingMethod` | `Smoothed` | Suavização secundária aplicada aos valores curtos %K e %D. |
| `ShortSmoothingLength` | `int` | 5 | Comprimento do filtro de suavização secundária para o oscilador curto. |
| `ShortEnableOpen` | `bool` | `true` | Permitir à estratégia abrir posições curtas. |
| `ShortEnableClose` | `bool` | `true` | Permitir à estratégia fechar posições curtas. |
| `TradeVolume` | `decimal` | 0.1 | Volume base para entradas de posição. |
| `TakeProfitPoints` | `decimal` | 2000 | Distância do take-profit expressa em pontos de preço. |
| `StopLossPoints` | `decimal` | 1000 | Distância do stop-loss expressa em pontos de preço. |

## Notas

- O `SmoothingMethod` adicional imita a suavização opcional baseada em JJMA do indicador original usando as médias móveis padrão disponíveis no StockSharp. Escolha `None` para desabilitar esta etapa se replicação exata não for necessária.
- Os módulos longo e curto são independentes; você pode habilitar ou desabilitar qualquer lado usando os flags booleanos correspondentes.
- Como o StockSharp opera com posições líquidas, a estratégia sempre fecha a exposição oposta quando um novo sinal inverte a direção.
