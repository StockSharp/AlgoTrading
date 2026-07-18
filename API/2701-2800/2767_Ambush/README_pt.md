# Estratégia de Emboscada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Emboscada cerca continuamente o mercado com um par de ordens buy-stop e sell-stop. As ordens pendentes são colocadas
a uma indentação configurável acima do melhor ask e abaixo do melhor bid, com uma substituição dinâmica que impõe uma distância
mínima baseada no spread atual. Sempre que um lado é acionado, a estratégia reconstrói imediatamente ambas as ordens para que o
mercado permaneça "em emboscada" de ambas as direções. Um disjuntor simples baseado em patrimônio pode nivelar as posições assim que
um alvo de lucro diário ou limite de perda é atingido.

Esta implementação em C# replica o comportamento do especialista original do MetaTrader 5 de Zuzabush. Opera puramente com cotações
de Nível 1 e não requer velas ou indicadores. Cada decisão é impulsionada por mudanças em tempo real do bid/ask, portanto a estratégia
é mais adequada para instrumentos líquidos com spreads apertados.

## Lógica de trading

1. **Recepção de dados de mercado**
   - A estratégia assina atualizações de Nível 1 e rastreia o último melhor bid e best ask.
   - Os cálculos param até que ambos os lados do livro de ordens estejam disponíveis e a estratégia tenha permissão para negociar.
2. **Salvaguardas de patrimônio**
   - O PnL realizado (`PnL`) e o componente não realizado derivado do bid/ask atual e `PositionPrice` são somados.
   - Se o patrimônio combinado exceder `EquityTakeProfit`, ou cair abaixo de `-EquityStopLoss`, a posição líquida atual é nivelada
     com uma ordem a mercado. As ordens pendentes são deixadas intactas, correspondendo ao comportamento original do especialista.
3. **Colocação de ordens pendentes**
   - O spread em unidades de preço é comparado com `MaxSpreadPoints`. Se o spread for muito amplo, nenhuma nova ordem é colocada.
   - Caso contrário, uma distância é calculada como `max(IndentationPoints * step, spread * 3)`. Esse valor replica a lógica MT5 de
     respeitar a indentação do usuário ou impor três spreads quando o `StopsLevel` do corretor é zero.
   - Uma ordem buy-stop é colocada em `ask + distância` e uma sell-stop em `bid - distância`. Os preços são normalizados para o
     tick mais próximo. Apenas uma ordem ativa por lado é permitida; ordens obsoletas são limpas quando seu estado muda para
     `Done`, `Failed` ou `Canceled`.
4. **Trailing de ordens pendentes**
   - Quando `TrailingStopPoints` é maior que zero, a estratégia recalcula periodicamente (não mais frequentemente que `Pause`) a
     distância de stop usando `max((TrailingStopPoints + TrailingStepPoints) * step, spread * 3)` e re-registra as ordens se a
     mudança exceder meio tick.
   - O trailing mantém as ordens próximas ao mercado enquanto ainda respeita a distância mínima que evita o acionamento prematuro.

O resultado final é um motor de rompimento tipo grid que está constantemente esperando que o preço se mova decisivamente em qualquer direção.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `IndentationPoints` | Distância base em pontos entre o mercado e cada ordem stop pendente. |
| `MaxSpreadPoints` | Spread máximo permitido (em pontos). As ordens são suspensas enquanto o spread for mais amplo. |
| `TrailingStopPoints` | Distância base de trailing em pontos aplicada a ordens pendentes existentes. Definir como zero para desativar o trailing. |
| `TrailingStepPoints` | Buffer adicional adicionado acima da distância base de trailing. |
| `Pause` | Tempo mínimo entre dois recálculos de trailing. O padrão espelha a pausa de um segundo do especialista MT5. |
| `EquityTakeProfit` | Lucro de patrimônio em moeda da conta que aciona um nivelamento imediato de posição. |
| `EquityStopLoss` | Queda de patrimônio permitida antes que a posição aberta seja fechada. |
| `Volume` | Tamanho da ordem herdado da classe base `Strategy`. Usar o mínimo do corretor para imitar o padrão MT5. |

Todos os offsets de preço são convertidos de pontos para unidades de preço reais usando `Security.PriceStep`. Se o instrumento não
expõe um passo de preço, um valor de fallback de 1 é usado.

## Notas práticas

- Como a estratégia trabalha apenas com ordens stop, nenhuma vela ou indicador é necessário. Pode ser executada durante backtests que
  não fornecem velas históricas desde que os dados de Nível 1 estejam disponíveis.
- Os corretores que impõem um `StopsLevel` não nulo devem configurar `IndentationPoints` para que a diferença de preço resultante
  satisfaça a regra da bolsa. A rede de segurança de triplo spread atua como guarda secundária.
- O filtro de patrimônio é intencionalmente leve e não cancela ordens pendentes. Isso espelha o comportamento original de Emboscada,
  permitindo novas negociações após o evento de nivelamento sem intervenção manual.
- O deslizamento e a tolerância de preenchimento de ordens são controlados pelo corretor ou simulador conectado. Ajustar `Volume` e
  valores de parâmetros para corresponder à volatilidade do instrumento.

Esta documentação fornece intencionalmente o nível máximo de detalhe para que tanto traders discricionários quanto algorítmicos possam
entender a conversão e personalizar a estratégia para seu local de execução.
