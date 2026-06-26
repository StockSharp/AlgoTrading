# Estratégia Vortex Indicator Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o expert do MetaTrader **Exp_VortexIndicator_Duplex** para a API de alto nível do StockSharp. Dois fluxos independentes do indicador Vortex são mantidos: um governa as operações compradas e o outro governa as operações vendidas. Cada fluxo pode usar seu próprio período, comprimento do indicador e deslocamento de barra, permitindo comportamento assimétrico entre configurações altistas e baixistas.

## Como funciona

1. Duas assinaturas de candles são abertas de acordo com `LongCandleType` e `ShortCandleType`. Cada feed atualiza sua própria instância de `VortexIndicator`.
2. Em cada candle finalizado, a estratégia registra os valores mais recentes de VI+ e VI-. Os parâmetros `LongSignalBar`/`ShortSignalBar` definem quantos candles fechados para trás devem ser usados para avaliação de sinal, correspondendo à entrada `SignalBar` do MetaTrader.
3. **Entrada comprada** – permitida quando `AllowLongEntries = true`. Uma ordem de compra é enviada se o valor atual de VI+ do fluxo longo estiver acima de VI-, enquanto o valor amostrado anterior tinha VI+ menor ou igual a VI-. Qualquer exposição vendida existente é encerrada antes de estabelecer a nova posição comprada.
4. **Saída comprada** – habilitada através de `AllowLongExits`. A posição comprada é fechada quando o valor VI- do fluxo longo sobe acima de VI+. Além disso, níveis de stop-loss e take-profit protetores expressos em passos de preço (`LongStopLossSteps`, `LongTakeProfitSteps`) são monitorados em cada candle; atingir qualquer limiar também fecha a operação.
5. **Entrada vendida** – governada por `AllowShortEntries`. Uma ordem de venda é colocada quando o VI+ do fluxo curto cai abaixo de VI- depois de anteriormente estar acima. A exposição comprada existente é encerrada durante a reversão.
6. **Saída vendida** – controlada por `AllowShortExits`. A posição vendida é coberta quando VI+ sobe novamente acima de VI-. Distâncias protetoras (`ShortStopLossSteps`, `ShortTakeProfitSteps`) fecham a operação se atingidas.
7. O dimensionamento de posição usa o parâmetro `TradeVolume`. A estratégia depende do `PriceStep` do instrumento para converter contagens de passos em distâncias de preço absolutas; definir um parâmetro de passo como zero desativa a regra de proteção correspondente.

As verificações de stop/take são avaliadas em cada candle finalizado de ambos os períodos. Se a conta não tiver posição, os dados de entrada armazenados são limpos para espelhar a implementação do MetaTrader.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `LongCandleType` | H4 | Período usado para o indicador Vortex do lado comprado. |
| `ShortCandleType` | H4 | Período usado para o indicador do lado vendido. |
| `LongLength` | 14 | Período de VI aplicado ao fluxo longo. |
| `ShortLength` | 14 | Período de VI aplicado ao fluxo curto. |
| `LongSignalBar` | 1 | Deslocamento de candle fechado para avaliação longa (0 = barra finalizada atual). |
| `ShortSignalBar` | 1 | Deslocamento de candle fechado para avaliação curta. |
| `AllowLongEntries` | true | Habilita a abertura de posições compradas. |
| `AllowLongExits` | true | Habilita o fechamento de posições compradas. |
| `AllowShortEntries` | true | Habilita a abertura de posições vendidas. |
| `AllowShortExits` | true | Habilita o fechamento de posições vendidas. |
| `LongStopLossSteps` | 1000 | Distância de stop-loss para operações compradas, expressa em passos de preço. |
| `LongTakeProfitSteps` | 2000 | Distância de take-profit para operações compradas, expressa em passos de preço. |
| `ShortStopLossSteps` | 1000 | Distância de stop-loss para operações vendidas, expressa em passos de preço. |
| `ShortTakeProfitSteps` | 2000 | Distância de take-profit para operações vendidas, expressa em passos de preço. |
| `TradeVolume` | 1 | Tamanho base da ordem de mercado usado ao entrar em uma posição. |

## Notas de execução

- A estratégia fecha qualquer posição oposta antes de abrir uma nova, reproduzindo efetivamente o comportamento do MT5, onde números mágicos separados gerenciavam sinais comprados e vendidos.
- Distâncias protetoras são convertidas via `distance = steps * Security.PriceStep`. Certifique-se de que o instrumento tenha um passo de preço válido; caso contrário, a estratégia usa 1.0 como fallback.
- Defina qualquer parâmetro de stop/take como zero para desabilitar esse caminho de proteção enquanto mantém as saídas baseadas em sinais ativas.
- Como ambos os períodos podem acionar o gerenciamento de risco, escolha `TradeVolume` cuidadosamente para evitar reversões repetidas em mercados com baixa liquidez.
