# Estratégia VininI Trend LRMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Acompanha o ponto final `L` da regressão linear móvel dos fechamentos concluídos. Padrões: `CandleType = 4 horas`, `Period = 13`, `UpLevel = 10`, `DnLevel = -10`, `Mode = Breakdown`. Aguarda indicadores formados e permissão para negociar.

Em **Breakdown**, os limites aplicam-se ao oscilador relativo `O = 100 × (Close − L) / L`, em porcentagem. Compra com `previousO <= UpLevel && O > UpLevel`; vende com `previousO >= DnLevel && O < DnLevel`. Não são níveis absolutos do preço LRMA.

Em **Twist**, compra quando a inclinação do ponto final passa de negativa a positiva e vende na mudança inversa. Um trecho plano preserva a direção anterior não nula.

Compra com `Position <= 0` e vende com `Position >= 0`. Fecha primeiro a posição contrária executada e depois abre `Volume` na nova direção com ordens a mercado. Sinais na mesma direção não aumentam a posição. Sem stop-loss ou take-profit.
