# Estratégia de fuga do FT TIME BIGDOG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **FT TIME BIGDOG** é um sistema de breakout da sessão de Londres convertido do MetaTrader 4 consultor especialista `FT_TIME_BIGDOG.mq4` (diretório `MQL/9259`).
Ele mede o intervalo de consolidação que se forma entre as horas de início e término configuradas e, em seguida, coloca ordens de parada acima e abaixo desse intervalo quando a janela fecha.
A versão StockSharp mantém o comportamento original enquanto expõe parâmetros configuráveis para tempo de breakout, distância do pedido e gerenciamento de risco.

## Lógica de negociação
1. Em cada dia de negociação, a estratégia registra a máxima mais alta e a mínima mais baixa das velas finalizadas, cujo horário de abertura fica entre **StartHour** e **StopHour** (inclusive).
2. Após o término da vela da hora de parada, se o intervalo acumulado for menor que **RangeLimitPoints**, duas ordens de parada pendentes tornam-se elegíveis:
   - Um **stop de compra** na máxima registrada.
   - Um **stop de venda** na mínima registrada.
3. As ordens são criadas somente se o preço de mercado estiver pelo menos **OrderBufferPoints** distante do nível de entrada. Os melhores preços de compra/venda são usados ​​quando disponíveis, caso contrário, o último fechamento da vela é usado.
4. Cada ordem pendente inclui um stop de proteção no lado oposto do intervalo e uma compensação de take-profit definida por **TakeProfitPoints**.
5. Quando uma posição é aberta, a ordem pendente oposta é cancelada. A posição ativa é monitorada nas velas finalizadas: se o preço tocar o stop loss armazenado ou o nível de lucro, a posição é fechada no mercado.
6. O ciclo é executado no máximo uma vez por dia; todo o estado é redefinido no início do próximo dia de negociação.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `StartHour` | 14 | Hora (0–23) marcando o início da janela de acumulação. |
| `StopHour` | 16 | Hora em que os pedidos pendentes se tornam elegíveis. Deve ser maior ou igual a `StartHour`. |
| `RangeLimitPoints` | 50 | Largura máxima do intervalo da sessão em pontos do corretor (pontos × `PointMultiplier`). Nenhum pedido será feito se o intervalo for maior. |
| `TakeProfitPoints` | 50 | Distância de take-profit aplicada às posições acionadas, expressa em pontos da corretora. |
| `OrderBufferPoints` | 20 | Distância mínima exigida entre o preço de mercado e uma ordem pendente. Evita que os pedidos sejam feitos muito próximos do preço atual. |
| `PointMultiplier` | 1 | Multiplicador aplicado ao tamanho do ponto do instrumento. Defina como 10 para símbolos forex de cinco dígitos. |
| `Volume` | 0,1 | Volume de pedidos para ambas as ordens stop. |
| `CandleType` | 1 hora | Série de velas usada para medir o alcance e avaliar o sinal de acionamento. |

## Gestão de Risco e Comércio
- Stop loss para negociações longas é igual ao mínimo da sessão; stop loss para negociações curtas é igual à alta da sessão.
- Os níveis de lucro são calculados a partir do preço de ruptura usando `TakeProfitPoints` e o tamanho do ponto do instrumento.
- Todos os controles de risco são executados em eventos de fechamento de velas; excursões intrabar além dos níveis de parada podem resultar em saídas atrasadas.

## Diferenças vs. Expert Advisor Original
- A versão MetaTrader opera em eventos de tick enquanto esta porta depende de velas concluídas e atualizações de nível 1. O comportamento dentro de uma vela pode, portanto, diferir ligeiramente.
- A conversão de pontos usa `Security.PriceStep` multiplicado por `PointMultiplier`. Verifique esta combinação antes de executar ao vivo.
- Os pedidos são feitos somente quando `StartHour <= StopHour`. As janelas da meia-noite não são suportadas nesta porta.

## Notas de uso
1. Atribua a segurança desejada e verifique se os dados de nível 1 estão disponíveis para verificações precisas do buffer.
2. Configure o horário de negociação de acordo com o fuso horário da corretora.
3. Execute primeiro a simulação para validar a conversão de pontos e o tempo em relação ao seu feed de dados.
4. Redefina ou interrompa a estratégia antes de alterar manualmente as ordens pendentes para evitar estados conflitantes.

## Arquivos
- `CS/FtTimeBigdogStrategy.cs` – implementação central de StockSharp com comentários embutidos detalhados.
- `MQL/9259/FT_TIME_BIGDOG.mq4` – fonte MetaTrader original usada para a conversão.
