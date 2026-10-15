# Estratégia de cambista AK-47
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão do consultor especialista MetaTrader 5 **"AK-47 Scalper EA" (build 44883)**. Ele recria o comportamento original dentro da estrutura estratégica de alto nível StockSharp.

O algoritmo mantém uma única ordem de *sell stop* ativa durante o horário de negociação permitido. Assim que a ordem é acionada, a estratégia anexa imediatamente ordens protetoras de stop-loss e take-profit. Tanto o preço da ordem pendente quanto o stop de proteção são reduzidos dinamicamente à medida que o mercado se move.

## Lógica principal

1. Calcule o tamanho do pip a partir do tamanho do tick do instrumento (símbolos de 5 dígitos usam etapas de 0,1 pip, assim como em MetaTrader).
2. Determine a janela de negociação. Quando o filtro de horário está habilitado, as entradas são permitidas apenas entre os horários de início e término configurados (inclusive o início, excluindo o término). As sessões noturnas são suportadas por volta da meia-noite.
3. Certifique-se de que o spread atual em pontos não ultrapasse o limite configurado antes de fazer novos pedidos.
4. Dimensione a posição:
   - Use o lote fixo (parâmetro `Base Lot`) ou
   - Converta o `Risk Percent` configurado do valor do portfólio em lotes (imitando a fórmula MT5) e alinhe-o com as restrições de volume de troca.
5. Coloque uma ordem stop de venda `SL/2` pips abaixo do lance. O stop de proteção está planejado `SL/2` pips acima do pedido e o take-profit fica `TP` pips abaixo da entrada.
6. Enquanto a ordem está pendente, a estratégia a registra novamente continuamente para manter o gap do SL/2 pip em relação à oferta e atualiza os preços de proteção planejados.
7. Após a execução:
   - Registre uma ordem buy-stop stop-loss e uma ordem buy-limit take-profit usando os preços planejados.
   - Em cada fechamento de vela, a estratégia segue o stop, mantendo-o exatamente `SL` pips acima do lance atual (nunca afrouxando-o).
   - O preço de realização do lucro permanece fixo depois de definido.
8. Se a posição for plana, todas as ordens de proteção serão canceladas e um novo ciclo poderá começar.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| **Usar porcentagem de risco** | Alterne entre lotes fixos e dimensionamento baseado em capital. |
| **Porcentagem de risco** | Percentual aplicado sobre o valor da carteira no cálculo do volume de negócios. |
| **Lote Básico** | Tamanho do lote fixo e etapa de arredondamento para dimensionamento da posição. |
| **Stop Loss (pips)** | Distância entre o preço de entrada e o stop de proteção. O deslocamento da ordem pendente usa metade dessa distância. |
| **Take Profit (pips)** | Distância alvo de lucro. Defina como zero para desabilitar o alvo. |
| **Spread máximo (pontos)** | Spread máximo permitido (em MetaTrader pontos) para entrar no mercado. |
| **Usar filtro de tempo** | Ative ou desative a restrição da janela de negociação. |
| **Hora/minuto de início** | Início da janela de negociação. |
| **Hora/minuto final** | Fim da janela de negociação. |
| **Tipo de vela** | Assinatura de velas usada para atualizações de prazos e preços. |

## Notas

- A estratégia usa apenas entradas curtas como o original EA.
- O rastreamento é realizado próximo à vela para permanecer sincronizado com o StockSharp API de alto nível.
- As ordens de proteção são substituídas por meio de chamadas `ReRegisterOrder`, portanto, a exchange ou simulador deve suportar a substituição de ordens.
- Os comentários gráficos originais de MetaTrader não são reproduzidos porque as estratégias de StockSharp dependem de registro em vez de comentários de terminal.
