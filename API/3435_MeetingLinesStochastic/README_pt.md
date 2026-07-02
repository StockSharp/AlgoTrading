# Estratégia de Linhas de Reunião Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Linhas de Encontro Stochastic** é uma implementação StockSharp do especialista MetaTrader *Expert_AML_Stoch*. Ele combina os padrões de reversão do castiçal das Linhas de Reunião de Alta/Baixa com a confirmação da linha de sinal %D do oscilador Stochastic. A estratégia foi projetada para traders discricionários que desejam uma abordagem baseada em regras para reconhecimento de padrões com confirmação adicional de impulso. Ao usar o StockSharp API de alto nível, o código permanece conciso, testável e fácil de estender para gerenciamento de portfólio ou automação adicional.

## Lógica de negociação

1. **Filtro de padrão de vela**
   - A estratégia avalia continuamente as duas últimas velas concluídas para detectar a formação de Linhas de Encontro.
   - Uma configuração de alta requer uma vela preta longa seguida por uma vela branca longa cujo preço de fechamento esteja dentro de 10% do fechamento anterior.
   - Uma configuração de baixa requer uma vela branca longa seguida por uma vela preta longa com o mesmo alinhamento próximo de 10%.
   - O tamanho médio do corpo da vela é calculado com uma média móvel simples configurável para filtrar corpos fracos.

2. **Stochastic Confirmação**
   - A linha de sinal %D do oscilador Stochastic deve confirmar o sinal da vela.
   - As entradas de alta exigem que %D esteja abaixo do limite de sobrevenda configurável (padrão 30).
   - As entradas de baixa exigem que %D esteja acima do limite de sobrecompra configurável (padrão 70).

3. **Regras de saída**
   - As posições curtas são fechadas quando %D cruza para cima através do nível de saída inferior (padrão 20) ou do nível de saída superior (padrão 80).
   - As posições longas são fechadas quando %D cruza os mesmos níveis para baixo.
   - As ordens de reversão fecham automaticamente a exposição existente e abrem uma nova posição na direção oposta.

4. **Manuseio de Volume**
   - A estratégia utiliza a propriedade base `Volume` quando esta é positiva; caso contrário, o padrão é um único lote para compatibilidade com o comportamento de lote fixo de MetaTrader.

## Parâmetros

| Nome | Descrição | Padrão | Notas |
| ---- | ----------- | ------- | ----- |
| `CandleType` | Série de velas primárias usadas para análise. | Período de 15 minutos | Aceita qualquer `DataType` compatível com StockSharp. |
| `StochasticLength` | Período de lookback para o cálculo %K bruto. | 3 | Espelha o MetaTrader `%K period`. |
| `StochasticSmoothing` | Suavização aplicada a %K (MetaTrader `slowing`). | 25 | Define o comprimento de suavização interna do oscilador. |
| `StochasticSignal` | Período de suavização para a linha de sinal %D. | 36 | Espelha o MetaTrader `%D period`. |
| `BodyAveragePeriod` | Número de velas usadas para calcular a média do tamanho do corpo da vela. | 3 | Filtra corpos menores ao detectar Linhas de Reunião. |
| `LongEntryLevel` | Valor máximo de %D que ainda permite uma entrada de alta. | 30 | Equivalente ao limite de sobrevenda. |
| `ShortEntryLevel` | Valor mínimo de %D necessário para uma entrada de baixa. | 70 | Equivalente ao limite de sobrecompra. |
| `ExitLowerLevel` | Limite inferior que aciona saídas em cruzamentos ascendentes. | 20 | Usado para decisões de saída longas e curtas. |
| `ExitUpperLevel` | Limite superior que aciona saídas em cruzamentos descendentes. | 80 | Usado para decisões de saída longas e curtas. |

Todos os parâmetros são expostos por meio do `StrategyParam<T>` e podem ser otimizados diretamente no StockSharp Designer ou programaticamente.

## Geração de Sinal

- **Entrada longa**: Linhas de reunião de alta + %D abaixo de `LongEntryLevel` sem exposição longa existente (as posições vendidas são invertidas).
- **Entrada curta**: Linhas de reunião de baixa + %D acima de `ShortEntryLevel` sem exposição curta existente (as posições compradas são revertidas).
- **Saída longa**: %D cruza abaixo de `ExitUpperLevel` ou `ExitLowerLevel`.
- **Saída curta**: %D cruza acima de `ExitLowerLevel` ou `ExitUpperLevel`.

## Notas de implementação

- Os dados dos indicadores são tratados via `BindEx`, evitando o gerenciamento manual de coleta de indicadores.
- A média do corpo da vela usa um `SimpleMovingAverage` alimentado com tamanhos absolutos de corpo por meio de `DecimalIndicatorValue`, correspondendo ao auxiliar MetaTrader `AvgBody`.
- Todos os comentários no código são escritos em inglês e o recuo depende de caracteres de tabulação de acordo com as diretrizes do projeto.
- A estratégia desenha automaticamente velas e o oscilador estocástico quando uma área do gráfico está disponível, simplificando o monitoramento ao vivo.

## Dicas de uso

1. **Otimização**: Use os parâmetros expostos para testes walk-forward para alinhar os limites com o instrumento negociado.
2. **Gerenciamento de riscos**: coloque a estratégia em camadas com o `StartProtection` integrado do `StartProtection` ou controles de risco externos em nível de portfólio para implantações de produção.
3. **Qualidade dos dados**: Os padrões das Linhas de Reunião são sensíveis a preços precisos de abertura/fechamento; garantir o alinhamento do feed e a filtragem de sessões ilíquidas.
4. **Períodos**: embora o padrão seja 15 minutos, dados intradiários ou diários podem ser usados modificando `CandleType`.

A estratégia oferece uma abordagem disciplinada para traders que dependem de formações de velas, mas exigem a confirmação do oscilador para reduzir falsos positivos.
