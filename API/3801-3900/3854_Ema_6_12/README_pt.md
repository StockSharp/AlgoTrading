# EMA Estratégia cruzada de 12/06
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o consultor especialista MetaTrader que negocia o cruzamento entre um EMA(6) rápido e um EMA(12) lento. Ele assina velas horárias por padrão, calcula ambas as médias móveis e aguarda um cruzamento confirmado no fechamento de uma vela antes de agir.

## Lógica de negociação

- **Entrada:**
  - Um sinal de alta aparece quando EMA(6) cruza acima de EMA(12). A estratégia abre uma posição longa se não houver posição ativa.
  - Um sinal de baixa aparece quando EMA(6) cruza abaixo de EMA(12). A estratégia abre uma posição curta se não houver posição ativa.
- **Sair:**
  - Quando `UseCloseSignals` está habilitado (comportamento padrão), a estratégia fecha a posição atual assim que um cruzamento oposto for detectado. Ele aguarda o próximo cruzamento antes de abrir uma nova negociação, espelhando o consultor especialista original.
  - As proteções opcionais de take-profit e trailing stop são gerenciadas por meio do auxiliar `StartProtection` integrado do StockSharp.
- **Dimensionamento da posição:**
  - Os pedidos usam o parâmetro `OrderVolume` (padrão 1 lote). Os volumes são alinhados às configurações de segurança antes do envio dos pedidos.

## Gestão de risco

- **Trailing stop:** Converte a configuração original de "pontos" em etapas de preço. Quando maior que zero, o stop segue automaticamente na direção da negociação assim que a posição se tornar lucrativa.
- **Take Profit:** Expresso em etapas de preço. Defina como zero para desativar.
- A estratégia nunca faz médias baixas ou pirâmides. Apenas uma posição por símbolo é permitida.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Período usado para construir velas e EMAs. O padrão é 1 hora. |
| `OrderVolume` | Tamanho comercial em lotes. |
| `ShortEmaLength` | Período para o EMA rápida (padrão 6). |
| `LongEmaLength` | Período para o EMA lenta (padrão 12). |
| `UseCloseSignals` | Fecha a posição atual em um crossover oposto (padrão: habilitado). |
| `TrailingStopSteps` | Distância final em etapas de preço. Zero desativa o rastreamento. |
| `TakeProfitSteps` | Calcule a distância do lucro nas etapas de preço. Zero o desativa. |

## Notas

- Os sinais são processados apenas em velas acabadas para evitar ruído intrabarra.
- Os valores EMA anteriores são redefinidos sempre que a posição retorna a zero, garantindo uma detecção limpa para o próximo cruzamento.
- Todos os comentários do código são escritos em inglês e o recuo usa tabulações de acordo com as diretrizes do projeto.
