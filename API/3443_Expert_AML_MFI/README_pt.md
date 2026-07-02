# Estratégia especializada em AML IMF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Expert AML MFI** replica o MetaTrader 5 consultor especialista "Expert_AML_MFI" usando o StockSharp API de alto nível. Ele se concentra no padrão de velas *Meeting Lines* e valida cada sinal com o oscilador **Money Flow Index (MFI)**. A estratégia mantém automaticamente as estatísticas de velas necessárias, identifica reversões de alta ou baixa e gerencia posições abertas sempre que a IMF ultrapassa os limites de sobrevenda ou sobrecompra.

## Lógica de negociação
1. **Preparação de velas** – a estratégia segue o período de tempo selecionado (H1 por padrão) e mantém as duas últimas velas concluídas junto com a média móvel dos corpos das velas. O tamanho médio do corpo é calculado através de um `SimpleMovingAverage` aplicado ao tamanho absoluto do corpo da vela, espelhando a implementação MT5.
2. **Detecção de padrões** – dois ajudantes especializados reconhecem *Linhas de reunião de alta* e *Linhas de reunião de baixa*:
   - Configuração de alta: uma vela longa de baixa seguida por uma vela longa de alta que fecha perto do fechamento anterior (dentro de 10% do corpo médio).
   - Configuração de baixa: uma vela longa de alta seguida por uma vela longa de baixa com preços de fechamento semelhantes.
3. **Confirmação da IMF** – o valor anterior da IMF deve estar abaixo do nível de entrada de alta (padrão 40) para negociações longas ou acima do nível de entrada de baixa (padrão 60) para negociações curtas.
4. **Gerenciamento de posição** – as duas últimas leituras de MFI são rastreadas para detectar cruzamentos dos níveis de sobrevenda (30) e sobrecompra (70):
   - Uma cruz acima de qualquer nível sai das posições curtas.
   - Uma cruz abaixo do nível de sobrevenda ou acima do nível de sobrecompra sai das posições longas.
5. **Execução de ordem** – quando ocorre um padrão válido e confirmação de IMF, a estratégia fecha qualquer exposição oposta e abre uma nova posição no mercado com o volume base configurado.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Prazo usado para assinatura de velas. | Período de 1 hora |
| `MfiPeriod` | Número de barras do oscilador MFI. | 12 |
| `BodyAveragePeriod` | Comprimento da janela para cálculo do tamanho médio do corpo. | 4 |
| `BullishEntryLevel` | Valor máximo de MFI permitido para entradas otimistas. | 40 |
| `BearishEntryLevel` | Valor mínimo de IMF exigido para entradas de baixa. | 60 |
| `OversoldLevel` | Nível de sobrevenda usado para sinais de saída. | 30 |
| `OverboughtLevel` | Nível de sobrecompra usado para sinais de saída. | 70 |
| `TradeVolume` | Volume base do pedido aplicado a novas negociações. | 1 |

Todos os parâmetros podem ser otimizados diretamente dentro do StockSharp Designer graças aos wrappers `StrategyParam`.

## Indicadores e recursos visuais
- **Índice de Fluxo de Dinheiro** – vinculado à assinatura da vela para confirmação e exibido no gráfico quando uma área do gráfico estiver disponível.
- **Média Móvel Simples dos corpos das velas** – apenas para uso interno, reproduzindo o cálculo do corpo médio MT5.

## Notas
- A estratégia chama `StartProtection()` uma vez para ativar recursos integrados de proteção de posição.
- Os comandos de negociação usam ajudantes `BuyMarket` e `SellMarket` para nivelar a posição atual antes de abrir uma nova, correspondendo ao comportamento do consultor especialista MetaTrader.
- Nenhuma porta Python é fornecida de acordo com os requisitos do projeto.
