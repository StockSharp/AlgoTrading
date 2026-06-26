# Estratégia Gap DM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Gap DM é uma estratégia contrária de negociação de gaps que rastreia a distância entre o fechamento da sessão anterior e a abertura da próxima sessão. Quando o mercado abre com um gap visível, a estratégia opera imediatamente na direção oposta, esperando que o preço reverta e preencha o gap. A implementação segue o algoritmo original MetaTrader 5 "Gap DM" de cmillion, adaptado à API de alto nível do StockSharp. Todas as decisões de negociação são derivadas de velas concluídas do período selecionado, garantindo comportamento determinístico em backtests e execução ao vivo.

## Lógica de Sinal
1. Assinar a série de velas especificada por `CandleType`.
2. Aguardar que cada vela termine (`CandleStates.Finished`).
3. Comparar o preço de fechamento da vela anterior com o preço de abertura da vela atual.
4. Calcular o tamanho do gap em pips usando o passo de preço do instrumento. Um multiplicador de 10 é aplicado automaticamente para cotações de 3 e 5 dígitos, reproduzindo a conversão ponto-para-pip do MT5.
5. Se a abertura atual estiver **abaixo** do fechamento anterior por pelo menos `Minimum Gap (pips)`, tratar como um gap baixista e **entrar comprado**.
6. Se a abertura atual estiver **acima** do fechamento anterior por pelo menos `Minimum Gap (pips)`, tratar como um gap altista e **entrar vendido**.
7. Pular entradas quando a negociação não é permitida (por exemplo, a estratégia está desconectada ou ainda em aquecimento).

## Dimensionamento de Posição e Limites
- `Order Volume` especifica o tamanho do lote para cada nova operação. A estratégia também usa o valor para fechar ou reverter exposição existente, mantendo a posição líquida consistente com o modelo de contabilidade líquida do StockSharp.
- `Max Positions` define o volume máximo agregado (em lotes) que pode ser mantido em uma direção. Quando o limite é atingido, novas entradas na mesma direção são ignoradas.
- Ao reverter de vendido para comprado (ou vice-versa), a estratégia adiciona automaticamente o volume necessário para fechar a exposição oposta antes de abrir a nova posição.

## Gestão de Risco
- `Stop Loss (pips)` coloca um stop protetor relativo ao preço de entrada. O stop é avaliado em cada vela concluída. Se o intervalo da vela tocar o nível de stop, a posição é fechada imediatamente com uma ordem de mercado.
- `Take Profit (pips)` funciona simetricamente ao stop-loss. Definir o parâmetro como zero para desativar o alvo.
- Nenhum trailing stop é aplicado por padrão; a lógica de saída corresponde ao Consultor Especialista fonte.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `Order Volume` | Volume de negociação usado para cada entrada em lotes. | `1` |
| `Stop Loss (pips)` | Distância do stop protetor. Definir como `0` para desativar. | `0` |
| `Take Profit (pips)` | Distância do alvo de lucro. Definir como `0` para desativar. | `0` |
| `Minimum Gap (pips)` | Diferença mínima entre o fechamento anterior e a abertura atual necessária para gerar um sinal. | `1` |
| `Max Positions` | Exposição acumulada máxima permitida em uma única direção (em lotes). | `15` |
| `Candle Type` | Período usado para medir gaps de sessão. | `1 Hora` |

## Fluxo de Execução
1. Redefinir o estado em cache em cada reinicialização (limiares de gap, níveis de stop, fechamento anterior).
2. Iniciar assinatura de velas e desenhar elementos do gráfico (velas e operações) quando uma área de gráfico estiver disponível.
3. Em cada vela terminada:
   - Atualizar ou redefinir o stop ativo e o alvo dependendo da posição atual.
   - Avaliar as condições de gap e colocar ordens de mercado quando um sinal válido aparecer.
   - Verificar novamente as ordens protetoras para que os eventos de stop-loss ou take-profit dentro da mesma vela sejam tratados sem atraso.
4. Armazenar o último fechamento para a próxima avaliação.

## Notas e Diferenças vs. a Versão MT5 Original
- As estratégias StockSharp operam com posições líquidas. O algoritmo emula múltiplas entradas escalando a exposição líquida em vez de criar tickets separados.
- Todos os comentários no código-fonte estão em inglês, em conformidade com as diretrizes do projeto.
- O gerenciamento de dinheiro via percentual de risco (modo `risk` no script MT5) não é reproduzido; em vez disso, um parâmetro de volume fixo é fornecido.

## Requisitos
- Compatível com qualquer instrumento que exponha um `PriceStep` válido.
- Funciona com velas baseadas em tempo, volume ou intervalo suportadas pelo StockSharp, desde que o conceito de gap seja significativo.
- Requer ambiente StockSharp capaz de executar ordens de mercado e monitorar operações próprias.
