# Estratégia Trailing Stop Manager
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria o controlador de trailing stop do especialista MetaTrader `MQL/17263/TrailingStop.mq5`. Ela se concentra em automatizar o gerenciamento de stop-loss após uma entrada já ter sido aberta.

## Ideia original
- **Fonte**: O especialista TrailingStop de Vladimir Karputov para contas de hedge.
- **Conceito**: No primeiro tick o EA abria posições tanto compradas quanto vendidas, depois ajustava os níveis de stop-loss de forma independente para cada lado usando distâncias baseadas em pips.
- **Objetivo**: Demonstrar como fazer trailing de stops com uma distância de ativação configurável e um passo de atualização.

## Adaptação ao StockSharp
- **Compatibilidade com netting**: As estratégias do StockSharp operam na posição líquida, portanto este port gerencia uma direção por vez. Para fazer trailing de ambos os lados simultaneamente, inicie duas instâncias da estratégia.
- **Atualizações baseadas em ticks**: A estratégia assina ticks de trades (`DataType.Ticks`) para espelhar os ajustes dirigidos por ticks do MetaTrader.
- **Conversão de pips**: Multiplica os valores de pip configurados por `Security.PriceStep` (retorna para 1 se a bolsa não fornecer um passo) para converter entradas em offsets de preço absolutos.
- **Auto-entrada opcional**: Um parâmetro permite enviar uma ordem de mercado imediata ao iniciar, o que é conveniente para demonstrações rápidas ou testes manuais.

## Lógica de trading
1. **Inicialização**
   - Lê o passo de preço do instrumento e assina dados de ticks.
   - Opcionalmente envia uma ordem de mercado de acordo com o parâmetro `Initial Direction`.
2. **Rastreamento de entrada**
   - Cada trade próprio reinicia o estado de trailing e armazena o preço de execução real como nova referência.
3. **Ativação**
   - Para posições compradas, o motor de trailing se ativa apenas após o preço avançar `Trailing Stop (pips)` desde a entrada. Para vendidas, requer uma queda equivalente.
4. **Ajuste do stop**
   - Uma vez ativado, o nível do stop equivale ao preço do tick atual menos/mais a distância de ativação.
   - O stop é movido apenas se o último tick o empurrar adiante pelo menos `Trailing Step (pips)`.
   - Um passo zero significa que o stop é atualizado em cada tick favorável.
5. **Saída**
   - Quando o preço retorna ao nível de trailing ou vai além, a estratégia fecha a posição restante com uma ordem de mercado.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| **Trailing Stop (pips)** | Distância de ativação em pips. Deve ser maior que zero. |
| **Trailing Step (pips)** | Movimento favorável mínimo em pips antes de avançar o stop novamente. Pode ser zero. |
| **Initial Direction** | Ordem de mercado opcional colocada durante `OnStarted` (`None`, `Long`, `Short`). |

## Notas adicionais
- O especialista original usava valores de bid/ask. Esta versão em C# usa o último preço de trade como uma boa aproximação, que é suficiente para a maioria dos instrumentos líquidos.
- Não há lógica de take-profit ou nova entrada. Você pode combinar este componente com outra estratégia de sinais ou iniciá-lo manualmente após abrir uma posição.
- Se o corretor fornece passos de pip fracionários, garanta que `Security.PriceStep` os reflita; caso contrário, ajuste os valores de pip para corresponder ao tamanho real do tick.
- Não há testes automatizados para este módulo, então valide em um feed de demonstração antes de implantar capital real.
