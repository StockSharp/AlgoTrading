# Estratégia do painel de troca de símbolos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Painel de Troca de Símbolos** é uma conversão StockSharp do painel MQL *"Painel de Troca de Símbolos"*. O especialista original atuou como um widget de gráfico que permitia aos traders digitar um símbolo, mudar o gráfico ativo para esse símbolo e monitorar informações de mercado em tempo real, como valores de OHLC, volume de ticks e spread. A estratégia convertida recria o mesmo fluxo de trabalho no ambiente StockSharp. Ele pode ser iniciado em qualquer título e fornece uma alternância manual para pular para outro instrumento enquanto registra continuamente as métricas de mercado mais relevantes.

## Comportamento central
- Assina dados de velas e cotações de nível um para o título ativo.
- Registra cada vela concluída com abertura, máximo, mínimo, fechamento, volume total e o último spread computado.
- Armazena cotações de compra/venda e obtém um spread atualizado que reflete a leitura do painel MQL.
- Reage a solicitações de troca manual e substitui a segurança monitorada pelo identificador escolhido sem exigir a reinicialização da estratégia.
- Mantém a segurança previamente selecionada para que trocas redundantes sejam ignoradas e ativações duplas acidentais não interrompam as assinaturas.

## Parâmetros
| Nome | Tipo | Descrição |
| --- | --- | --- |
| `TargetSecurityId` | `string` | Identificador de segurança que deve ser ativado quando a solicitação de troca for acionada. Strings vazias são ignoradas com um aviso. |
| `CandleType` | `DataType` | Agregação de velas para atualizações periódicas (o padrão é velas de 1 hora, replicando o período do painel MQL). |
| `SwapRequested` | `bool` | Sinalizador manual que solicita uma mudança imediata para `TargetSecurityId`. Ele é redefinido para `false` após o processamento da tentativa de troca. |

## Assinaturas de dados
- Assinatura Candle criada com `CandleType` para a segurança atualmente ativa.
- Assinatura de nível um usada para rastrear cotações de compra/venda e calcular um valor de spread ao vivo.
- As assinaturas são reiniciadas com segurança sempre que a segurança muda, garantindo que fluxos de dados obsoletos não sejam deixados em execução.

## Fluxo de trabalho
1. Quando a estratégia é iniciada ela resolve a segurança inicial de `Strategy.Security` ou, se faltar, de `TargetSecurityId`.
2. Assinaturas de vela e nível um são abertas para esse instrumento.
3. Cada vela concluída aciona uma mensagem de registro detalhada que reflete o texto mostrado nos rótulos originais do painel.
4. As atualizações de nível um recebidas atualizam os valores de compra/venda em cache.
5. Definir `SwapRequested` como `true` e fornecer um `TargetSecurityId` válido alterna imediatamente a segurança monitorada e reinicia as assinaturas.

## Notas de uso
- A estratégia foi projetada para monitoramento manual e não realiza pedidos.
- O spread só é reportado quando os valores de compra e venda estão presentes e positivos.
- Quando um símbolo inválido ou desconhecido é fornecido, um aviso é registrado e a solicitação é descartada sem interromper as assinaturas em execução.
- Como a ferramenta original atualizava a IU uma vez por segundo, você pode diminuir o período da vela se precisar de atualizações de log mais frequentes.

## Recursos originais MQL preservados
- Troca manual de símbolos através de um identificador textual.
- Exibição em tempo real de valores, volume e spread de OHLC para o símbolo escolhido.
- Salvaguardas contra entradas vazias e adições falhadas do Market Watch (traduzidas em StockSharp avisos).

## Diferenças da implementação MQL
- A estratégia StockSharp usa mensagens de registro em vez de rótulos na tela. Isso corresponde ao fluxo de trabalho típico dentro de StockSharp enquanto ainda expõe as mesmas informações.
- A troca de gráficos é implementada reatribuindo a segurança da estratégia e recriando assinaturas em vez de alterar uma janela de gráfico do terminal.
- A lógica de atualização baseada em temporizador é substituída por eventos de conclusão de vela para permanecer alinhado com APIs StockSharp de alto nível.

## Requisitos
- Conector StockSharp com acesso aos títulos desejados.
- Feed de dados de nível um para obter cotações de compra/venda para cálculo de spread.
