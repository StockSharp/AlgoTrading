# Fim de semana do Tuyul Gap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Tuyul Gap End Of Week transporta o MetaTrader 5 consultor especialista `TuyulGAP` para StockSharp. A estratégia se prepara para a abertura semanal do mercado, examinando um número configurável de velas recentes na noite de sexta-feira, colocando um par de ordens de breakout stop em torno da máxima mais alta e da mínima mais baixa. É permitida apenas uma sessão de negociação por semana; uma vez que as ordens são preparadas, a estratégia espera que o preço passe por qualquer um dos níveis. Qualquer posição aberta que atinja uma meta de lucro segura na moeda da conta é fechada imediatamente e todas as ordens pendentes restantes são canceladas na segunda-feira para redefinir o fluxo de trabalho para a próxima semana.

## Lógica estratégica
* **Acionador de sessão semanal** – a configuração é executada em um dia da semana configurável (sexta-feira por padrão) quando o relógio da exchange atinge a hora configurada. Durante a janela de minuto (23h00-23h15 por padrão), a estratégia prepara os níveis de breakout uma vez por sessão.
* **Níveis de rompimento dinâmico** – a máxima mais alta e a mínima mais baixa das velas concluídas `Lookback Bars` anteriores definem os preços de gatilho. Buy Stop é colocado um tick acima da máxima, Sell Stop um tick abaixo da mínima, imitando o deslocamento do ponto MetaTrader.
* **Higiene de ordem pendente** – se já existir uma ordem de parada para a semana, ela não será recriada. A ordem pendente oposta permanece ativa após um lado ser acionado, de modo que a estratégia pode negociar em qualquer direção do gap.
* **Saída segura de lucro** – as posições abertas são monitoradas em cada vela finalizada. Quando o lucro não realizado de uma posição atinge a meta de lucro segura (na moeda do portfólio), ele é achatado no mercado, independentemente da direção.
* **Reinicialização semanal** – na primeira vela de segunda-feira, a estratégia cancela quaisquer ordens pendentes ainda ativas e rearma o sinalizador de sessão para que a configuração da próxima sexta-feira possa ser preparada.

## Parâmetros
* **Volume** – volume de pedidos para ordens de breakout stop.
* **Stop Loss (pontos)** – distância do preço de entrada, expressa em pontos do instrumento, usada para colocar um stop de proteção após a abertura de uma posição. Defina como `0` para desativar a parada.
* **Barras Lookback** – número de velas finalizadas inspecionadas para calcular os níveis máximos e mínimos semanais.
* **Configuração do dia da semana** – índice do dia (0=Domingo… 6=Sábado) que aciona a configuração semanal. O valor padrão de `5` mantém o comportamento original de sexta-feira.
* **Hora de Configuração** – hora de câmbio usada como âncora para preparar as ordens de breakout.
* **Janela de minutos de configuração** – número de minutos após `Setup Hour` quando a configuração permanece válida. Com o valor padrão `15` a estratégia é executada entre 23h e 23h15 inclusive.
* **Meta de lucro segura** – lucro mínimo não realizado por posição (na moeda do portfólio) que desencadeia uma saída imediata do mercado.
* **Tipo de vela** – período de tempo usado para varredura alta/baixa e loop de monitoramento.

## Notas adicionais
* A ordem de stop loss é enviada somente após a abertura de uma posição, porque StockSharp não suporta anexar um stop de proteção diretamente a uma ordem de stop pendente.
* Os níveis de volume, preço e stop são normalizados usando a etapa do título e as informações de precisão que StockSharp fornece.
* Não há tradução em Python para esta estratégia; apenas a implementação C# está incluída neste pacote.
