# Estratégia eKeyboardTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o comportamento do consultor especialista MetaTrader "eKeyboardTrader" usando o StockSharp API de alto nível. O script original ouvia atalhos de teclado para enviar ordens de mercado manuais e exibia texto auxiliar diretamente no gráfico. Na versão StockSharp as entradas interativas são expostas como parâmetros de estratégia enquanto a lógica de execução, verificações de segurança e recursos de proteção de ordem permanecem fiéis à implementação MQL.

## Lógica de negociação
1. **Assinatura de Nível 1** – a estratégia assina dados de mercado de Nível 1 para receber os melhores preços de compra e venda mais recentes. Essas cotações são necessárias antes que uma solicitação manual possa ser executada, imitando a dependência MetaTrader dos dados atuais do tick.
2. **Comandos manuais** – três parâmetros booleanos (`BuyRequest`, `SellRequest`, `CloseRequest`) representam os atalhos de teclado originais (B, S e C). Quando qualquer parâmetro é definido como `true` a estratégia executa a ação de mercado correspondente e zera imediatamente a bandeira.
3. **Limitação de taxa** – um tempo de espera de um segundo protege contra envios duplos acidentais, idêntico à verificação do temporizador implementada na versão MQL. As solicitações levantadas durante o resfriamento aguardam o próximo ciclo de processamento.
4. **Proteção de ordem** – distâncias opcionais de stop-loss e take-profit, expressas em MetaTrader pontos, são convertidas em preços absolutos usando `Security.PriceStep`. Quando pelo menos uma distância de proteção é configurada, a estratégia ativa a lógica `StartProtection` integrada do `StartProtection` para que cada entrada manual receba automaticamente as ordens de proteção configuradas.
5. **Reconhecimento de derrapagem** – o parâmetro `SlippagePoints` é preservado para compatibilidade e é mencionado no log sempre que um pedido manual é enviado, emulando os comentários informativos mostrados pelo consultor especialista.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `OrderVolume` | Volume base para ordens manuais de mercado. |
| `StopLossPoints` | Distância do preço de entrada até o stop de proteção em MetaTrader pontos. Defina como `0` para desativar. |
| `TakeProfitPoints` | Distância do preço de entrada até a meta de proteção em MetaTrader pontos. Defina como `0` para desativar. |
| `SlippagePoints` | Tolerância de derrapagem informativa exibida no log para cada pedido manual. |
| `BuyRequest` | Defina como `true` para enviar uma ordem de compra de mercado (redefinição automática após processamento). |
| `SellRequest` | Defina como `true` para enviar uma ordem de venda a mercado (redefinição automática após processamento). |
| `CloseRequest` | Defina como `true` para nivelar a posição líquida ao preço de mercado (redefinições automáticas após o processamento). |

## Diferenças da versão MQL
- Os avisos de texto no gráfico e as notificações sonoras não são reproduzidos. Em vez disso, as mensagens de registro documentam as ações executadas.
- As ordens de proteção são gerenciadas por meio do auxiliar `StartProtection` de StockSharp, que envia ordens de mercado quando o limite é atingido, em vez de modificar tickets MetaTrader individuais.
- A entrada do teclado é substituída por alternância de parâmetros. Qualquer UI que hospede a estratégia pode mapear as interações do usuário (teclado, botões, scripts) para esses parâmetros.
- Os diagnósticos de solicitação de negociação MetaTrader são condensados em instruções de registro para manter a conversão leve.

## Notas de uso
- Atribua `Security` e `Portfolio` antes de iniciar a estratégia; essas verificações refletem as condições de inicialização do consultor especialista.
- Os sinalizadores de comando manual são avaliados quando novos dados de Nível 1 chegam. Num mercado calmo, as ações são executadas na próxima cotação disponível.
- Ajustar `StopLossPoints` ou `TakeProfitPoints` enquanto a estratégia está em execução requer reiniciá-la para reconfigurar o módulo de proteção, correspondendo à configuração de proteção uma vez por sessão do script original.
