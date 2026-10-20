# Estratégia do Efeito de Janeiro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Negocia o efeito janeiro na virada do ano. Entra na primeira vela concluída disponível do primeiro dia de negociação a partir de 26 de dezembro. Sai ao final da primeira sessão a partir de 15 de janeiro ou na primeira vela posterior elegível se faltar o fechamento. Não repete janelas mensais nem abre posições vendidas.

Usa velas concluídas de 15 minutos e o calendário explícito de `Security.Board`, incluindo feriados, sessões especiais e fuso horário da praça. Sem calendário, a inicialização falha. Abre apenas posições compradas, com uma tentativa de entrada por temporada anual; após um stop, não entra novamente nessa temporada. Ordens pendentes bloqueiam decisões sobrepostas.

`StopLoss = 2` configura proteção nativa local de 2% a partir do preço real de execução. Atualizações bid/ask podem acionar saída a mercado entre velas. Não há take-profit.

A temporada é registrada ao enviar a entrada. A posição mantém o prazo de saída de janeiro dessa temporada mesmo que a próxima vela disponível chegue apenas em dezembro de um ano posterior; encerra-se nessa vela, sem passar para outra temporada.
