# Estratégia Gridder EA (portada do MQL4)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O GridderEA original é um expert advisor de grid trading multi-símbolo projetado para MetaTrader 4. Este port StockSharp mantém os conceitos centrais — espaçamento progressivo, dimensionamento adaptativo de lote, take-profit de cesta e hedge de emergência — enquanto foca em um único instrumento gerenciado pela estratégia hospedeira. A estratégia assina um fluxo de candles configurável, observa barras concluídas e abre operações de média quando o preço se afasta do último nível de referência por uma distância definida em pips.

## Lógica de negociação
1. **Progressão do grid:** um passo base (em pips) define o movimento mínimo de preço exigido antes de colocar uma nova operação. Cada ordem adicional pode escalar esse passo geometricamente ou exponencialmente para ampliar o grid quando a volatilidade aumenta.
2. **Progressão de lote:** a primeira ordem usa o volume inicial. Ordens subsequentes multiplicam o volume anterior conforme o modo configurado de progressão de lote (estático, geométrico ou exponencial).
3. **Metas da cesta:** lucro e perda não realizados são medidos na moeda da conta combinando o desvio de preço de cada operação aberta com o valor do passo do instrumento. Quando o lucro total excede a meta de lucro por lote, todas as posições são fechadas. Da mesma forma, uma meta de perda por lote pode liquidar a cesta como stop protetor.
4. **Modo de emergência:** quando o número de operações de um lado atinge o gatilho de emergência, a estratégia opcionalmente abre uma operação de hedge dimensionada como fração do volume acumulado. Isso imita o "Emergency Mode" da versão MQL e ajuda a limitar drawdowns.
5. **Proteção de posição:** `StartProtection()` é invocado na inicialização para garantir que a estratégia base monitore mudanças inesperadas de posição e resincronize com o estado da bolsa.

A implementação StockSharp evita manipular grandes coleções históricas e processa apenas candles concluídos, espelhando o comportamento do expert original em barras concluídas.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| **Initial Volume** | Volume da primeira ordem do grid. |
| **Volume Multiplier** | Fator aplicado para calcular o próximo volume quando a progressão de lote é geométrica ou exponencial. |
| **Grid Step (pips)** | Distância base (em pips) entre entradas sucessivas. |
| **Step Multiplier** | Fator de escala do espaçamento do grid quando a progressão de passo é geométrica ou exponencial. |
| **Target Profit / Lot** | Meta de lucro não realizado expressa por lote. Alcançar esta meta fecha todas as operações abertas. |
| **Target Loss / Lot** | Limiar de perda não realizada por lote. Quando alcançado, todas as operações são fechadas para conter drawdown. |
| **Max Orders Per Side** | Limita o número de operações de média permitidas em cada lado do mercado. `0` desativa o limite. |
| **Allow Long / Allow Short** | Habilita ou desabilita pernas compradas/vendidas independentemente. |
| **Step Mode** | Determina como o passo cresce: estático, geométrico ou exponencial. |
| **Lot Mode** | Determina como o volume da ordem cresce: estático, geométrico ou exponencial. |
| **Use Emergency Mode** | Habilita a lógica de hedge que protege contra cestas grandes demais. |
| **Emergency Trigger** | Número de ordens em um lado que ativa o hedge. |
| **Hedge Volume Factor** | Fração do volume total do lado colocada como ordem de hedge quando o modo de emergência dispara. |
| **Candle Type** | Timeframe da assinatura de candles usada para cálculos do grid. |

## Diferenças em relação ao EA original
- O port gerencia um único ativo por vez; anexe múltiplas instâncias da estratégia para negociar vários instrumentos, replicando o comportamento multi-símbolo do expert MQL.
- Painéis de tela e anotações de gráfico do MetaTrader não são reproduzidos; use áreas de gráfico StockSharp para visualizar candles e operações próprias se desejar.
- Presets de gestão monetária e perfis detalhados de fechamento parcial são simplificados na lógica unificada de lucro/perda da cesta.

## Notas de uso
1. Configure o tipo de candle, volume e espaçamento do grid nos parâmetros do construtor (pela UI ou interface de otimização).
2. Inicie a estratégia quando o ativo estiver conectado a uma bolsa real ou simulada. A estratégia assina automaticamente os candles selecionados.
3. Monitore o gatilho de emergência e o fator de hedge para ajustar a agressividade da fase de recuperação. Um fator de hedge maior traz a posição líquida de volta ao neutro mais rápido, mas reduz rentabilidade.
4. Combine com controles de risco StockSharp (proteção de carteira, monitor de posição máxima etc.) para segurança adicional.

## Exemplo de hedge de emergência
Suponha que a estratégia abriu cinco ordens compradas de média com volumes progressivamente maiores. Se o gatilho de emergência estiver em cinco e o fator de hedge em 0,5, no momento em que a quinta compra executar a estratégia enviará uma venda automática a mercado do tamanho de metade do volume comprado total. Isso espelha a lógica MQL que trava parcialmente a cesta e espera uma saída por reversão à média.

## Dicas de otimização
- Otimize **Grid Step (pips)** e **Volume Multiplier** juntos; passos pequenos exigem multiplicadores conservadores para evitar exposição descontrolada.
- Use **Target Profit / Lot** para traduzir metas em dólares do MetaTrader para o ambiente StockSharp sem depender de histórico de operações fechadas.
- Ajuste **Emergency Trigger** e **Hedge Volume Factor** de acordo com a volatilidade do instrumento negociado. Volatilidade maior geralmente se beneficia de hedge mais cedo.

## Recomendações de segurança
- Teste extensivamente no simulador antes de implantar em produção.
- Monitore tamanhos de contrato específicos da corretora para garantir que o volume arredondado corresponda à granularidade real do lote.
- Combine com regras de stop-out (por exemplo, via robô hospedeiro) para evitar perdas catastróficas em mercados de tendência onde grids podem acumular posições grandes.
