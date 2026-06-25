# Estratégia de Vela de Mudança Média
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia convertida do especialista MetaTrader `Exp_AverageChangeCandle`. Recria a lógica original dentro do StockSharp suavizando ratios de velas relativos a uma média móvel de referência dinâmica e reagindo às transições de cor altista/baixista.

## Ideia principal

1. Calcular uma média móvel de referência (`MaMethod1`, `Length1`) sobre o preço aplicado selecionado.
2. Expressar o preço de abertura e fechamento da vela atual como ratios em relação à referência e elevá-los à potência `Power`.
3. Suavizar os valores transformados de abertura e fechamento com uma segunda média móvel (`MaMethod2`, `Length2`).
4. Classificar a cor da vela: altista quando o fechamento suavizado &gt; abertura suavizada, baixista quando o fechamento suavizado &lt; abertura suavizada.
5. Gerar sinais de trading quando a cor muda após o atraso `SignalBar` configurado.

Apenas velas terminadas são processadas. A estratégia abre posições de mercado na direção da nova cor e opcionalmente fecha o lado oposto.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `OrderVolume` | `1` | Volume usado ao abrir uma nova posição. |
| `MaMethod1` | `Lwma` | Suavização aplicada ao ratio de referência (subconjunto de SMA/EMA/SMMA/LWMA/JJMA/AMA). Tipos não suportados usam EMA. |
| `Length1` | `12` | Período da média móvel de referência. |
| `Phase1` | `15` | Parâmetro de fase Jurik para a referência (mantido por compatibilidade). |
| `PriceSource` | `Median` | Preço aplicado antes de calcular a referência. |
| `MaMethod2` | `Jjma` | Suavização aplicada aos ratios transformados. |
| `Length2` | `5` | Período da média móvel de sinal. |
| `Phase2` | `100` | Parâmetro de fase Jurik para a suavização de sinal. |
| `Power` | `5` | Expoente usado ao elevar os ratios de abertura/fechamento. |
| `SignalBar` | `1` | Quantas velas fechadas esperar antes de agir em uma mudança de cor. |
| `BuyOpenEnabled` | `true` | Permitir abrir posições compradas. |
| `SellOpenEnabled` | `true` | Permitir abrir posições vendidas. |
| `BuyCloseEnabled` | `true` | Fechar comprados quando um sinal baixista aparece. |
| `SellCloseEnabled` | `true` | Fechar vendidos quando um sinal altista aparece. |
| `StopLossPoints` | `0` | Distância absoluta de stop-loss. `0` desativa o stop. |
| `TakeProfitPoints` | `0` | Distância absoluta de take-profit. `0` desativa o alvo. |
| `CandleType` | Período `H4` | Série de velas processada pela estratégia. |

## Regras de trading

- **Transição altista** (`color` muda para 2): fechar vendidos ativos (se permitido) e abrir uma posição comprada quando `Position <= 0` e `BuyOpenEnabled` é verdadeiro.
- **Transição baixista** (`color` muda para 0): fechar comprados ativos (se permitido) e abrir uma posição vendida quando `Position >= 0` e `SellOpenEnabled` é verdadeiro.
- Cor 1 (neutra) não aciona operações.
- Os sinais são avaliados usando a barra localizada `SignalBar` passos atrás da vela terminada mais recente para imitar o timing original do MetaTrader.

## Gestão de risco

`StopLossPoints` e `TakeProfitPoints` configuram `StartProtection` com distâncias absolutas. Quando qualquer valor é zero, a proteção respectiva é desativada.

## Notas

- Apenas os métodos de suavização disponíveis no StockSharp são implementados diretamente. JurX, ParMA, T3 e VIDYA do código original são mapeados para EMA como alternativa funcional.
- Os parâmetros de fase são mantidos por compatibilidade, mas afetam apenas médias baseadas em Jurik/Kaufman.
- A estratégia usa ordens de mercado assim como o assessor especialista original. O gerenciamento de slippage da versão MQL não é reproduzido porque o StockSharp lida com a execução via conectores.
