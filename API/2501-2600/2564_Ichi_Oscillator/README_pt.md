# Estratégia Ichi Oscilador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do especialista MetaTrader 5 **Exp_ICHI_OSC** para a API de alto nível do StockSharp.
- Opera em uma série de velas configurável e deriva sinais de um oscilador construído sobre linhas de Ichimoku.
- O valor bruto do oscilador é `((Close - SenkouA) - (Tenkan - Kijun)) / Step`, suavizado por uma média móvel selecionável.
- As ordens são executadas com o volume da estratégia; blocos complexos de gerenciamento de dinheiro do código original foram substituídos pelo gerenciamento de posições do StockSharp.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período de velas usado para todos os cálculos de indicadores. |
| `IchimokuBase` | Período base que define os comprimentos de Tenkan (`base * 0.5`), Kijun (`base * 1.5`) e Senkou B (`base * 3`). |
| `Smoothing Method` | Média móvel usada para suavizar o oscilador. Opções: `Simple`, `Exponential`, `Smoothed`, `Weighted`, `Jurik`, `Kaufman`. |
| `Smoothing Length` | Período do método de suavização selecionado. |
| `Smoothing Phase` | Parâmetro de compatibilidade reservado (mantido da versão MQL, atualmente não usado pelas implementações de suavização integradas). |
| `Signal Bar` | Número de barras para trás a partir da última vela terminada usado para ler as cores do oscilador (padrão `1`). |
| `Enable Buy Entries / Enable Sell Entries` | Permitir abrir posições compradas ou vendidas respectivamente. |
| `Enable Buy Exits / Enable Sell Exits` | Permitir fechar posições compradas ou vendidas existentes. |
| `Stop Loss (points)` | Distância de stop protetora expressa em passos de preço. |
| `Take Profit (points)` | Distância de take-profit expressa em passos de preço. |
| `Order Volume` | Volume base de ordem utilizado pelas ordens de mercado. |

## Lógica de negociação
1. Subscrever a série de velas solicitada e calcular os valores de Tenkan, Kijun e Senkou A usando os períodos de Ichimoku derivados.
2. Construir o oscilador a partir das diferenças entre o preço, Senkou A, Tenkan e Kijun e passá-lo pelo suavizador selecionado.
3. Atribuir um código de cor a cada valor suavizado:
   - `0` — oscilador acima de zero e subindo.
   - `1` — oscilador acima de zero e caindo.
   - `2` — neutro (nível zero ou inalterado).
   - `3` — oscilador abaixo de zero e decrescendo.
   - `4` — oscilador abaixo de zero e subindo.
4. Ler duas cores: a barra em `SignalBar + 1` (cor anterior) e a barra em `SignalBar` (cor atual).
   - Se a cor anterior é `0` ou `3`, fechar vendidos quando permitido e abrir um comprado quando a cor atual é `2`, `1` ou `4`.
   - Se a cor anterior é `4` ou `1`, fechar comprados quando permitido e abrir um vendido quando a cor atual é `0`, `1` ou `3`.
5. As ordens são colocadas com o volume configurado. Comprados e vendidos nunca são acumulados: os sinais de abertura são avaliados somente após a lógica de saída ter rodado na mesma barra.

## Gestão de risco
- As ordens protetoras são gerenciadas pelo `StartProtection`, usando as distâncias de stop loss e take profit em passos de preço.
- Nenhum trailing ou saídas parciais estão habilitados por padrão.

## Notas
- O módulo de gerenciamento de dinheiro original (cálculos de lote, tratamento de desvio, timers de operações) é substituído pelo controle de posição e volume do StockSharp.
- Métodos de suavização que não existem no StockSharp (p.ex., JurX, ParMA, VIDYA, T3) não estão disponíveis; escolher a alternativa mais próxima da lista fornecida.
- Os timestamps de sinal nos logs incluem o tempo de fechamento da vela mais um período completo de vela, refletindo o uso de `TimeShiftSec` no MQL.
