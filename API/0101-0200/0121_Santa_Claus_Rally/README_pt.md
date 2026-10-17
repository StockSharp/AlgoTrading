# Estratégia Santa Claus Rally
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Negocia o rali de fim de ano. Entra na primeira vela concluída elegível das últimas cinco sessões de dezembro. Sai ao final da segunda sessão de janeiro ou na primeira vela posterior elegível se a vela de fechamento estiver ausente.

Usa velas concluídas de 15 minutos e o calendário explícito de `Security.Board`, incluindo feriados, sessões especiais e fuso horário da praça. Sem calendário, a inicialização falha. Abre apenas posições compradas, com uma tentativa de entrada por temporada anual; após um stop, não entra novamente nessa temporada. Ordens pendentes bloqueiam decisões sobrepostas.

`StopLoss = 2` configura proteção nativa local de 2% a partir do preço real de execução. Atualizações bid/ask podem acionar saída a mercado entre velas. Não há take-profit.
