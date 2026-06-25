# Estratégia CoensioTrader1 V06
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
CoensioTrader1 V06 é uma estratégia de seguidor de tendência com rompimento originalmente distribuída como um Expert Advisor do MetaTrader. O port para StockSharp mantém a lógica de reconhecimento de padrões discricionária enquanto remove as funcionalidades específicas do broker e da internet da implementação MQL. A estratégia opera em um único instrumento e período, usando Bollinger Bands e uma média móvel exponencial dupla (DEMA) para identificar movimentos de exaustão seguidos de retomada de tendência.

O robô original permitia negociar até seis pares de moedas com conjuntos de parâmetros individuais, suportava licenciamento baseado em DLL e reportava resultados de otimização para um servidor remoto. Esses serviços auxiliares são omitidos intencionalmente neste port. O foco é o fluxo central de entrada e saída que reage a rejeições de Bollinger Bands confirmadas pela estrutura de swing e pela inclinação do DEMA.

## Lógica da estratégia
1. **Subscrição de dados** – a estratégia se inscreve no tipo de candle configurado (padrão: 1 hora) e vincula Bollinger Bands juntamente com um DEMA.
2. **Rejeição de Bollinger Bands** – os sinais são avaliados no último candle completamente fechado.
   - **Configuração Comprado**
     - O candle abriu abaixo da Bollinger Band inferior anterior e fechou de volta acima dela (rompimento falso para baixo).
     - O candle criou uma mínima mais alta em comparação com a barra anterior, enquanto essa barra anterior fez uma mínima mais baixa em comparação com seu predecessor (estrutura estilo fundo duplo).
     - O DEMA está estritamente subindo ao longo das últimas três observações (valor atual > anterior > segundo anterior).
   - **Configuração Vendido**
     - O candle abriu acima da Bollinger Band superior anterior e fechou de volta abaixo dela (rompimento falso para cima).
     - O candle fez uma máxima mais baixa em comparação com a barra anterior, enquanto essa barra anterior fez uma máxima mais alta em comparação com seu predecessor (estrutura de topo duplo).
     - O DEMA está estritamente caindo ao longo das últimas três observações.
3. **Execução de ordens** – ordens de mercado são enviadas imediatamente após o sinal ser confirmado em um candle terminado. O achatamento opcional de posição em sinais opostos pode ser habilitado.
4. **Gestão de risco** – distâncias opcionais de stop-loss e take-profit são fornecidas através de `StartProtection`. Ambas são offsets de preço absoluto; a funcionalidade de trailing stop do expert original não é reproduzida.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | --------- | ------- |
| `BollingerPeriod` | Período para o cálculo das Bollinger Bands. | 30 |
| `BollingerDeviation` | Multiplicador de desvio padrão para as bandas. | 1.5 |
| `DemaPeriod` | Comprimento da média móvel exponencial dupla usada para confirmação de tendência. | 20 |
| `StopLossDistance` | Offset absoluto de stop-loss passado para `StartProtection`. Definir como zero para desabilitar. | `0 (absolute)` |
| `TakeProfitDistance` | Offset absoluto de take-profit passado para `StartProtection`. Definir como zero para desabilitar. | `0 (absolute)` |
| `CloseOnSignal` | Fechar a posição atual antes de abrir uma nova na direção oposta. | `false` |
| `CandleType` | Tipo de dado de candle ou período. Padrão é o período de 1 hora. | `1h` |

## Notas de uso
- A versão StockSharp negocia apenas o `Strategy.Security` principal. Para imitar o comportamento multi-símbolo do expert original, lance instâncias de estratégia separadas com conjuntos de parâmetros distintos.
- A lógica de dimensionamento de lotes MQL (`RiskMax`, `LotSize`, `LotBalanceDivider`) não foi traduzida. Configure `Volume` na estratégia ou via gerenciador de risco de acordo com as regras do seu portfólio.
- A ativação baseada em DLL, o registro remoto de otimização e as rotinas de desenho de UI presentes nos arquivos MQL foram intencionalmente removidos.
- Os valores de stop-loss e take-profit são distâncias de preço absolutas. Adapte-os ao tamanho do tick ou valor de pip do instrumento ao configurar a estratégia.
- O mecanismo original de passo de trailing-stop não está implementado. Se o gerenciamento de trailing for necessário, adicione um módulo de risco dedicado sobre esta estratégia.
- Todos os comentários do código e lógicas são mantidos em inglês conforme solicitado; as traduções do README são fornecidas separadamente.

## Diferenças em relação à versão MQL
- **Gestão multi-símbolo**: substituída por um design de instrumento único para maior clareza e testes mais fáceis.
- **Redes e licenciamento**: removidos; nenhuma solicitação HTTP externa ou chamada de DLL é realizada.
- **Dimensionamento de ordens**: simplificado para depender do tratamento padrão de `Volume` do StockSharp.
- **Objetos visuais**: anotações de gráfico do MetaTrader (setas, rótulos, temas de cor) não são recriados. Use os helpers de gráfico do StockSharp se visualização for necessária.
- **Trailing stop**: não portado; apenas as ordens protetoras iniciais são configuradas.

Esta documentação pretende ser exaustiva para que o port possa ser revisado, testado e estendido sem necessidade de ler o código fonte MQL original.
