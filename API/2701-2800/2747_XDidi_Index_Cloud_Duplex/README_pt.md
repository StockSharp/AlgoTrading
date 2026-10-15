# Estratégia XDidi Index Cloud Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia XDidi Index Cloud Duplex replica a lógica de sinalização dupla comprado/vendido do especialista MQL5 original *Exp_XDidi_Index_Cloud_Duplex*. Duas configurações independentes do índice XDidi são avaliadas em períodos configuráveis. Cada configuração calcula uma razão entre médias móveis rápidas/médias e lentas/médias. Os cruzamentos entre essas razões acionam entradas de mercado enquanto divergências persistentes acionam saídas.

## Lógica de trading
1. **Cálculo do indicador**
   - Três médias móveis são calculadas para cada bloco (rápida, média, lenta) em uma fonte de preço selecionada.
   - As razões XDidi são derivadas como `fast / medium` e `slow / medium`. A inversão opcional corresponde à opção original `Revers`.
2. **Geração de sinais**
   - Bloco comprado: quando a barra anterior tinha `fast > slow` e a barra de sinal fecha com `fast <= slow`, uma entrada comprada é solicitada. Se a barra anterior tinha `fast < slow`, uma saída comprada é solicitada.
   - Bloco vendido: quando a barra anterior tinha `fast < slow` e a barra de sinal fecha com `fast >= slow`, uma entrada vendida é solicitada. Se a barra anterior tinha `fast > slow`, uma saída vendida é solicitada.
   - Os offsets de barra de sinal reproduzem as entradas originais `SignalBar`.
3. **Gestão de ordens**
   - As entradas são executadas com o volume da estratégia. Posições opostas são fechadas antes de reverter.
   - Níveis opcionais de stop-loss e take-profit são aplicados via `StartProtection` usando distâncias de passo de preço.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `LongCandleType`, `ShortCandleType` | Períodos de velas para cada bloco. |
| `LongFastMethod` / `Medium` / `Slow` & `ShortFastMethod` / `Medium` / `Slow` | Métodos de suavização da média móvel para curvas rápida, média e lenta. Suavizadores legados não suportados revertem para a média exponencial. |
| `LongFastLength`, `LongMediumLength`, `LongSlowLength` | Períodos para as médias móveis do bloco comprado. |
| `ShortFastLength`, `ShortMediumLength`, `ShortSlowLength` | Períodos para as médias móveis do bloco vendido. |
| `LongAppliedPrice`, `ShortAppliedPrice` | Fonte de preço usada para cada bloco (fechamento, abertura, típico, Demark, etc.). |
| `EnableLongEntries`, `EnableShortEntries` | Alternar novas posições compradas/vendidas. |
| `EnableLongExits`, `EnableShortExits` | Alternar saídas automáticas. |
| `LongSignalBar`, `ShortSignalBar` | Deslocamento histórico (barras atrás) avaliado para cruzamentos. |
| `LongReverse`, `ShortReverse` | Inverter razões (espelha o flag `Revers` no MQL). |
| `StopLossPoints`, `TakeProfitPoints` | Distâncias de proteção expressas em passos de preço (definir como zero para desabilitar). |
| `Volume` (propriedade base da estratégia) | Define o tamanho de operação padrão. |

## Notas de implementação
- As médias móveis são tomadas da biblioteca de indicadores do StockSharp. Suavizadores avançados (`JJMA`, `JurX`, `ParMA`, `VIDYA`) usam suavização exponencial por padrão porque equivalentes diretos não estão disponíveis.
- Os valores do indicador são processados apenas em velas finalizadas, correspondendo ao comportamento original de `IsNewBar`.
- As filas de sinais mantêm apenas o número necessário de valores de razão históricos, evitando coleções pesadas.
- Os stops de proteção são opcionais; se ambas as distâncias forem zero, a estratégia ainda chama `StartProtection()` para cumprir o ciclo de vida do framework.

## Dicas de uso
- Alinhe os tipos de velas com a assinatura de dados disponível no seu conector.
- Otimize os comprimentos de médias móveis e preços aplicados para o instrumento negociado.
- Quando usar períodos assimétricos (comprado/vendido), ambas as assinaturas são visualizadas em áreas de gráfico separadas para maior clareza.

## Limitações em comparação com a versão MQL5
- Os modos de gestão de dinheiro (`MM`, `MarginMode`) não estão replicados; o tamanho de operação segue a propriedade `Volume` do StockSharp.
- Alguns algoritmos de suavização exóticos de `SmoothAlgorithms.mqh` são aproximados com médias móveis exponenciais.
- As ordens de stop/limite são convertidas em níveis de proteção genéricos em vez de parâmetros de ordens individuais.
