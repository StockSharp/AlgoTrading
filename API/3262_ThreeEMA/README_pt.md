# Estratégia de Three EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia reproduz o consultor especialista MetaTrader "ThreeEMA" empilhando três médias móveis exponenciais (EMAs). Ela busca alinhamento direcional entre uma EMA rápida, média e lenta no mesmo período. Quando as médias estão estritamente ordenadas de forma ascendente (rápida acima da média acima da lenta), a estratégia abre ou mantém uma posição comprada. Quando a ordem se inverte (rápida abaixo da média abaixo da lenta), ela abre ou mantém uma posição vendida. As compensações protetoras de stop-loss e take-profit espelham os parâmetros MQL originais e são expressas em pontos de preço relativos ao tamanho de tick do instrumento.

## Comportamento MQL original
A versão MQL instanciava três indicadores EMA (`FastPeriod`, `MediumPeriod`, `SlowPeriod`) e gerava sinais de trading baseados em sua ordenação relativa na barra fechada mais recentemente:

- **Abrir comprado / fechar vendido** quando `FastEMA > MediumEMA > SlowEMA`.
- **Abrir vendido / fechar comprado** quando `FastEMA < MediumEMA < SlowEMA`.
- Stop-loss e take-profit eram aplicados como distâncias fixas em pontos a partir do preço de entrada.

Ordens eram enviadas com execução de mercado e o bloco de gerenciamento de dinheiro usava um tamanho de lote fixo. O módulo de trailing estava desabilitado.

## Detalhes de implementação StockSharp
- Usa a API de assinatura de velas de alto nível. Três indicadores `ExponentialMovingAverage` estão vinculados à assinatura do período principal para que cada vela concluída entregue todos os valores de EMA simultaneamente.
- As decisões de trading são avaliadas apenas em velas completamente formadas para evitar ruído intrabarra.
- Sempre que um stack direcional aparece, a estratégia cancela quaisquer ordens vigentes, fecha a exposição oposta se necessário e abre uma nova posição de mercado na direção requerida.
- `StartProtection` converte as distâncias configuradas de stop-loss e take-profit em pontos em offsets de preço reais usando o `PriceStep` do instrumento. Isso espelha o comportamento protetor do EA original.
- A integração de gráficos desenha velas e as três EMAs quando uma área de gráfico está disponível, facilitando a validação visual de sinais.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `CandleType` | Período de 1 minuto | Período da assinatura de velas usada para EMAs. |
| `FastPeriod` | 5 | Comprimento da EMA rápida. Deve ser menor que `MediumPeriod`. |
| `MediumPeriod` | 12 | Comprimento da EMA média. Deve estar entre os períodos rápido e lento. |
| `SlowPeriod` | 24 | Comprimento da EMA lenta. Deve ser o maior valor de período. |
| `StopLossPoints` | 400 | Distância protetora de stop-loss expressa em pontos do instrumento (convertida para preço usando `PriceStep`). Zero para desabilitar. |
| `TakeProfitPoints` | 900 | Distância de take-profit em pontos do instrumento (convertida para preço usando `PriceStep`). Zero para desabilitar. |

## Notas de uso
1. Configure `Volume` antes de iniciar a estratégia para refletir o tamanho de ordem desejado (o EA original usava lotes fixos).
2. Certifique-se de que os períodos de EMA permaneçam estritamente crescentes; caso contrário, uma exceção é lançada durante `OnStarted` para corresponder à validação encontrada no código fonte MQL.
3. Como a lógica sempre inverte posições quando o stack EMA se reverte, a estratégia está continuamente exposta ao mercado quando as condições alternam entre alinhamentos altistas e baixistas.
