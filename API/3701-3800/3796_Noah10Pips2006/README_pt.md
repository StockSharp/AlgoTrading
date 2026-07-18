# Estratégia Noah 10 Pips 2006
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- Recria a lógica de ruptura e reversão do consultor especialista Noah10pips2006 MetaTrader 4 original.
- Constrói canais de preços da sessão anterior e coloca ordens stop em torno do ponto médio.
- Aplica rastreamento de lucro seguro, dimensionamento de posição dinâmico opcional e uma negociação de reversão opcional após o fechamento da primeira posição.

## Lógica de negociação
1. **Cálculo do intervalo de sessões**
No início de cada novo dia de negociação (após aplicar a compensação de fuso horário configurada), a estratégia registra a máxima e a mínima da sessão anterior. Esses níveis são usados para calcular:
   - O ponto médio entre alto e baixo.
   - Um buffer de "aprovação" posicionado 20 pips acima/abaixo do intervalo.
   - Um canal de entrada obtido subtraindo/adicionando 40 pips (ou 25% do intervalo se o intervalo for maior que 160 pips).
2. **Pedido inicial pendente**
Quando o mercado entra na janela de negociação, a estratégia verifica o último fechamento:
   - Se o fechamento estiver entre o ponto médio e o buffer superior, um **stop de venda** será colocado no ponto médio.
   - Se o fechamento estiver entre o buffer inferior e o ponto médio, um **buy stop** será colocado no ponto médio.
A largura do intervalo deve exceder o mínimo configurado antes de qualquer pedido ser feito.
3. **Segundo pedido pendente**
Se apenas uma ordem stop permanecer ativa, o sistema adiciona a ordem de direção oposta ao buffer correspondente (buffer superior para um stop de compra, buffer inferior para um stop de venda). Isso reflete o comportamento original EA e prepara a estratégia para rompimentos em ambos os lados do intervalo.
4. **Gerenciamento de posição**
   - As ordens protetoras de stop-loss e take-profit são criadas após o preenchimento de uma entrada.
   - Assim que o lucro flutuante atinge o limite de acionamento seguro, o stop loss é movido para bloquear o lucro seguro configurado.
   - Quando o bloqueio seguro está ativo, um trailing stop opcional continua acompanhando o preço na distância especificada.
5. **Desligamento diário**
Todas as ordens pendentes e posições abertas serão fechadas quando a janela de negociação terminar ou quando o limite de sexta-feira for atingido.
6. **Negociação de reversão**
A primeira posição concluída pode acionar uma ordem de mercado na direção oposta, reproduzindo o comportamento “reverso após stop” do código original. A reversão é ignorada se o ajuste de lucro seguro já tiver garantido ganhos.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Série de velas usada para conduzir cálculos e tempo. Padrão: velas de 1 hora. |
| `TimeZoneOffset` | Turno (em horas) aplicado para trocar carimbos de data/hora antes dos cálculos diários. |
| `StartHour`, `StartMinute` | Horário de abertura da janela de negociação no fuso horário alterado. |
| `EndHour`, `EndMinute` | Hora de fechamento da janela de negociação. Novas entradas não são colocadas posteriormente. |
| `FridayEndHour` | Hora na sexta-feira em que as posições são fechadas à força. |
| `TradeFriday` | Habilita ou desabilita a abertura de novas posições na sexta-feira. |
| `StopLossPips`, `TakeProfitPips` | Distância (em pips) das ordens de proteção criadas após a entrada. |
| `TrailingStopPips` | Distância do trailing-stop usada após a etapa de lucro seguro. Defina como 0 para desativar o rastreamento. |
| `SecureProfitPips` | Lucro bloqueado quando o gatilho seguro é ativado. |
| `TrailSecureProfitPips` | Limite de lucro necessário antes de mover o stop para o nível seguro. |
| `MinimumRangePips` | Largura mínima do canal de entrada necessária para realizar pedidos. |
| `StartYear`, `StartMonth` | Ignore os dados de mercado anteriores a esta data. |
| `MinVolume`, `MaxVolume` | Limites aplicados ao volume de pedido calculado. |
| `MaximumRiskPercent` | Porcentagem do valor do portfólio arriscado por negociação quando o dimensionamento dinâmico está habilitado. |
| `FixedVolume` | Quando `true`, a estratégia usa a propriedade `Volume` em vez do modelo de risco. |

## Notas práticas
- O instrumento deve fornecer valores `PriceStep` e `StepPrice` válidos quando o modo de dimensionamento de posição baseado em risco for usado.
- Os ajustes de lucro seguro e de rastreamento dependem de velas concluídas, portanto, os preenchimentos intrabarras são processados na próxima vela concluída.
- A estratégia cancela e substitui as ordens de proteção sempre que a lógica móvel atualiza o preço stop.
- Certifique-se de que o deslocamento do fuso horário corresponda à fonte dos dados históricos; caso contrário, o intervalo do dia anterior pode ser diferente do especialista MT4 original.

## Advertências de conversão
- Os objetos de desenho visual da versão MT4 foram omitidos; use os níveis fornecidos ou adicione anotações de gráfico personalizadas, se necessário.
- O algoritmo assume cotação Forex de quatro dígitos ao converter os buffers fixos de 20/40 pip; ajustar parâmetros para diferentes classes de ativos.
- As negociações reversas são executadas no mercado com o modelo de volume atual, correspondendo ao comportamento do EA original após a exclusão de ordens pendentes opostas.
