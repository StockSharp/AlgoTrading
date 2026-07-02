# Estratégia Auto KDJ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Auto KDJ é uma conversão direta do MetaTrader 4 consultores especialistas `AutoKdj.mq4` criados por *senlin ge*. O sistema negocia um único símbolo e avalia o oscilador estocástico suavizado conhecido como **KDJ** (também chamado de %K, %D, %J). A implementação do StockSharp recria a mesma lógica do indicador e as opções de gerenciamento de dinheiro expostas no consultor especialista original, ao mesmo tempo em que aproveita os recursos de alto nível do API, como assinaturas de velas, vinculação de indicadores e ordens de proteção automáticas.

KDJ é construído sobre o oscilador estocástico. Ele primeiro calcula um valor bruto Stochastic (RSV), suaviza-o na linha %K, suaviza %K novamente na linha %D e usa sua diferença (referida como *KDC* no código-fonte) para detectar mudanças no impulso. A Auto KDJ abre no máximo uma posição de mercado por vez e aplica imediatamente as proteções de stop-loss/take-profit solicitadas.

## Construção do Indicador
1. **Cálculo RSV** – Para cada vela finalizada, são coletadas a máxima mais alta e a mínima mais baixa em `KDJ Length` velas. O RSV é calculado como:
\[
RSV = \frac{\text{Fechar} - \text{LowestLow}}{\text{HighestHigh} - \text{LowestLow}} \times 100
\]
2. **Suavização %K** – Os valores RSV são calculados em média ao longo de `Smooth %K` períodos para obter a linha %K.
3. **Suavização %D** – A média dos valores %K é calculada em `Smooth %D` períodos para produzir a linha %D.
4. **Sinal KDJ** – O algoritmo analisa `K - D` (o buffer *KDC* da versão MQL) e a inclinação de %K para gerar entradas e saídas.

Este pipeline é implementado com o indicador `Stochastic` de StockSharp configurando o período e os parâmetros de suavização para espelhar os buffers MetaTrader.

## Regras de negociação
Os sinais são avaliados uma vez por vela finalizada. A estratégia se recusa a abrir outra posição enquanto houver uma negociação aberta ou uma ordem de saída pendente, o que corresponde ao comportamento do consultor especialista MQL.

### Condições de Entrada
- **Compre** quando uma das seguintes situações for verdadeira:
  - `K - D` cruza de negativo para positivo.
  - `K - D` já é positivo e %K está subindo (`K_current > K_previous`).
- **Vender** quando uma das seguintes situações for verdadeira:
  - `K - D` cruza de positivo para negativo.
  - `K - D` já é negativo e %K está caindo (`K_current < K_previous`).

### Condições de saída
- **Fecha longo** quando `K - D` cruza abaixo de zero ou quando %K começa a cair.
- **Feche a posição vendida** quando `K - D` ultrapassar zero ou quando %K começar a subir.

Quando a posição é achatada, a estratégia registra se a negociação foi lucrativa ou não. Perdas consecutivas influenciam o tamanho da próxima posição exatamente da mesma maneira que a lógica `DecreaseFactor` do MQL EA.

## Gestão de capital
O consultor especialista original fornece uma opção `whichmethod` para combinar o comportamento de stop-loss e take-profit, além de uma rotina dinâmica de tamanho de lote com base no uso de margem e faixas de perdas. A porta StockSharp reproduz esses recursos como parâmetros individuais:

- **Alterna stop-loss/take-profit** – Sinalizadores booleanos independentes permitem ativar ou desativar cada perna de proteção. Quando ativo, `StartProtection` anexa as saídas de proteção e lida com a execução do mercado.
- **Volume baseado em risco** – O tamanho do pedido começa em `Base Volume` e pode ser aumentado para satisfazer a fração solicitada de `Maximum Risk` do portfólio. O consumo de margem é aproximado através do tamanho do contrato do instrumento e da alavancagem configurada, que emula o cálculo MT4 `AccountFreeMargin * MaximumRisk * Leverage / 100000`.
- **Redução de sequência de perdas** – Após duas ou mais negociações consecutivas com perdas, o próximo pedido é reduzido em `volume * losses / DecreaseFactor`, correspondendo à rotina de redução de volume original.

Todos os volumes são normalizados usando os valores `VolumeStep`, `MinVolume` e `MaxVolume` do título para garantir que o tamanho do pedido enviado seja negociável.

## Parâmetros
| Parâmetro | Descrição | Padrão | Otimização |
|-----------|-------------|---------|--------------|
| **Tipo de vela** | Tipo de dados/período de velas de entrada. | Período de 15 minutos | – |
| **Comprimento KDJ** | Período de lookback para cálculo do RSV. | 30 | 10 → 60 passo 5 |
| **Suave %K** | Suavização aplicada à linha %K. | 3 | 1 → 10 passo 1 |
| **Suave %D** | Suavização aplicada à linha %D. | 6 | 1 → 15 passo 1 |
| **Stop Loss (pips)** | Distância para a parada de proteção. | 100 | 0 → 300 passo 10 |
| **Take Profit (pips)** | Distância para o lucro protetor. | 200 | 0 → 400 passo 10 |
| **Ativar Stop Loss** | Alterne para a perna de stop-loss. | Habilitado | – |
| **Ativar o Take Profit** | Alterne para a perna de lucro. | Habilitado | – |
| **Volume Básico** | Volume mínimo antes do ajuste de risco. | 0,1 | – |
| **Risco Máximo** | Fração do patrimônio alocado por negociação. | 0,4 | 0,0 → 1,0 passo 0,1 |
| **Fator de diminuição** | Redução de volume após estrias de perdas. | 0,3 | 0,0 → 5,0 passo 0,5 |
| **Alavancagem** | Alavancagem da conta usada no modelo de margem. | 100 | 10 → 500 passo 10 |

## Notas de uso
1. Configure a segurança e a conexão desejadas no StockSharp Designer, Shell ou Runner.
2. Ajuste o tipo de vela para corresponder ao período usado em MetaTrader.
3. Defina preferências de stop-loss/take-profit por meio das opções booleanas para reproduzir o comportamento `whichmethod`:
   - Desative ambas as pernas para "sem SL, sem TP".
   - Habilite apenas a perna take-profit ou stop-loss para espelhar os modos de proteção parcial.
4. Opcionalmente, ajuste `Base Volume`, `Maximum Risk`, `Decrease Factor` e `Leverage` para espelhar a configuração do seu corretor.
5. Comece a estratégia. O auxiliar gráfico traça automaticamente velas, o indicador KDJ e executa negociações para verificação.

## Diferenças em comparação com a versão MQL
- O indicador `kdj.mq4` personalizado foi substituído pelo indicador `Stochastic` integrado do StockSharp configurado para fornecer buffers idênticos, eliminando a necessidade de arquivos externos.
- O dimensionamento da posição usa o patrimônio do portfólio, o tamanho do contrato e a alavancagem fornecidos pela definição de segurança StockSharp. Corretores com diferentes multiplicadores de contrato podem ajustar `Base Volume` ou `Maximum Risk` de acordo.
- As saídas protetoras dependem de `StartProtection`, que envia ordens de mercado quando acionadas e registra o preço de preenchimento. Isso oferece o mesmo comportamento funcional que os parâmetros `OrderSend` + stop/take em MetaTrader, permanecendo idiomático para StockSharp.
- A redução do risco após perdas consecutivas é monitorada através de negociações executadas, em vez de verificar todo o histórico de negociações a cada tick, melhorando o desempenho e mantendo os resultados idênticos.

## Teste
A estratégia foi validada comparando os pontos de entrada/saída gerados com a lógica MQL original em dados de amostra EURUSD. Os comerciantes ainda devem executar testes ou otimização em seu ambiente de destino para confirmar se o porto se comporta conforme esperado com as especificações de contrato e modelo de execução de seu corretor.
